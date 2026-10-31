#include "arknova_vision.h"
#include <opencv2/core.hpp>
#include <opencv2/imgproc.hpp>
#include <opencv2/features2d.hpp>
#include <opencv2/calib3d.hpp>
#include <opencv2/imgcodecs.hpp>
#include <memory>
#include <stdexcept>
#include <algorithm>
#include <cmath>
#include <cstring>
#include <limits>
#include <vector>

static_assert(sizeof(anv_image) == 32, "ABI v1 supports 64-bit targets only");
static_assert(sizeof(anv_probe_result) == 104, "ABI v1 layout changed");
static_assert(CV_VERSION_MAJOR == 4 && CV_VERSION_MINOR == 13 && CV_VERSION_REVISION == 0,
              "The parity spike requires official OpenCV 4.13.0");

static bool valid(const anv_image* image) {
    // Dimension bounds are ABI resource guards, never detection thresholds.
    return image && image->data && image->reserved == 0 && image->width > 0 &&
        image->height > 0 && image->width <= 16384 && image->height <= 16384 &&
        image->stride >= int64_t(image->width) * 3 &&
        image->length >= uint64_t(image->stride) * uint64_t(image->height);
}

int32_t anv_abi_version(void) { return 1; }

static int32_t probe_impl(const anv_image* reference, const anv_image* photo,
                  anv_probe_result* result, uint8_t* warp, uint64_t capacity, double* inlier_area) {
    if (!result) return ANV_INVALID_INPUT;
    *result = {};
    if (!valid(reference) || !valid(photo) || !warp) return ANV_INVALID_INPUT;
    try {
        cv::Mat master(reference->height, reference->width, CV_8UC3,
                       const_cast<uint8_t*>(reference->data), reference->stride);
        cv::Mat image(photo->height, photo->width, CV_8UC3,
                      const_cast<uint8_t*>(photo->data), photo->stride);
        const double reference_scale = 1600.0 / std::max(master.cols, master.rows);
        const double photo_scale = std::min(1.0, 1600.0 / std::max(image.cols, image.rows));
        cv::Mat registration, small, half_master, reference_gray, photo_gray;
        cv::resize(master, registration, {}, reference_scale, reference_scale, cv::INTER_AREA);
        cv::resize(master, half_master, {}, 0.5, 0.5, cv::INTER_AREA);
        const uint64_t required = uint64_t(half_master.cols) * half_master.rows * 3;
        if (capacity < required) return ANV_BUFFER_TOO_SMALL;
        cv::resize(image, small, {}, photo_scale, photo_scale, cv::INTER_AREA);
        cv::cvtColor(registration, reference_gray, cv::COLOR_BGR2GRAY);
        cv::cvtColor(small, photo_gray, cv::COLOR_BGR2GRAY);
        std::vector<cv::KeyPoint> reference_points, photo_points;
        cv::Mat reference_descriptors, photo_descriptors;
        auto sift = cv::SIFT::create(6000);
        sift->detectAndCompute(reference_gray, cv::noArray(), reference_points, reference_descriptors);
        sift->detectAndCompute(photo_gray, cv::noArray(), photo_points, photo_descriptors);
        if (reference_descriptors.empty() || photo_descriptors.empty()) return ANV_REGISTRATION_FAILED;
        cv::BFMatcher matcher(cv::NORM_L2);
        std::vector<std::vector<cv::DMatch>> pairs;
        matcher.knnMatch(photo_descriptors, reference_descriptors, pairs, 2);
        std::vector<cv::Point2d> source, target;
        for (const auto& pair : pairs) {
            if (pair.size() == 2 && pair[0].distance < 0.72 * pair[1].distance) {
                source.emplace_back(photo_points[pair[0].queryIdx].pt);
                target.emplace_back(reference_points[pair[0].trainIdx].pt);
            }
        }
        if (source.size() < 24) return ANV_REGISTRATION_FAILED;
        cv::Mat inlier_mask;
        cv::Mat homography = cv::findHomography(source, target, cv::RANSAC, 3, inlier_mask);
        if (homography.empty() || !cv::checkRange(homography)) return ANV_REGISTRATION_FAILED;
        std::vector<cv::Point2d> projected;
        cv::perspectiveTransform(source, projected, homography);
        std::vector<double> errors;
        for (size_t index = 0; index < source.size(); ++index)
            if (inlier_mask.at<uint8_t>(int(index)) != 0)
                errors.push_back(cv::norm(projected[index] - target[index]));
        if (errors.empty()) return ANV_REGISTRATION_FAILED;
        std::sort(errors.begin(), errors.end());
        const double p95 = errors[size_t((errors.size() - 1) * 0.95)];
        if (errors.size() < 20 || double(errors.size()) / source.size() < 0.35 || p95 > 3)
            return ANV_REGISTRATION_FAILED;
        cv::Mat transform = homography.clone();
        for (int row = 0; row < 2; ++row)
            for (int col = 0; col < 3; ++col)
                transform.at<double>(row, col) *= 0.5 / reference_scale;
        if (inlier_area) {
            std::vector<cv::Point2f> inlier_targets, hull;
            for (size_t i=0;i<target.size();++i)
                if (inlier_mask.at<uint8_t>(int(i))) inlier_targets.emplace_back(float(target[i].x*0.5/reference_scale),float(target[i].y*0.5/reference_scale));
            cv::convexHull(inlier_targets,hull);
            *inlier_area=std::abs(cv::contourArea(hull));
        }
        cv::Mat warped;
        cv::warpPerspective(small, warped, transform, half_master.size());
        anv_probe_result success{};
        success.matches = int32_t(source.size());
        success.inliers = int32_t(errors.size());
        success.warp_width = warped.cols;
        success.warp_height = warped.rows;
        success.reprojection_p95 = p95;
        for (int row = 0; row < 3; ++row)
            for (int col = 0; col < 3; ++col)
                success.homography[row * 3 + col] = transform.at<double>(row, col);
        success.warp_bytes = required;
        // warpPerspective creates a contiguous owned Mat; copy only on success.
        std::memcpy(warp, warped.data, size_t(required));
        *result = success;
        return ANV_OK;
    } catch (const cv::Exception&) {
        return ANV_INTERNAL_ERROR;
    } catch (...) {
        return ANV_INTERNAL_ERROR;
    }
}

int32_t anv_probe(const anv_image* reference, const anv_image* photo,
    anv_probe_result* result, uint8_t* warp, uint64_t capacity) {
    return probe_impl(reference,photo,result,warp,capacity,nullptr);
}

namespace {
struct field_mask { int track, value; cv::Rect bounds; cv::Mat mask, core; };
struct engine { cv::Mat reference, half; std::vector<field_mask> fields; cv::Rect scoring; };
static_assert(sizeof(anv_evidence)==72 && sizeof(anv_analysis_result)==120, "Engine ABI layout changed");

cv::Mat decode(const uint8_t* bytes, uint64_t length) {
    if (!bytes || length==0 || length>INT32_MAX) throw std::invalid_argument("Encoded input length");
    cv::Mat data(1,int(length),CV_8UC1,const_cast<uint8_t*>(bytes));
    cv::Mat image=cv::imdecode(data,cv::IMREAD_COLOR);
    if (image.empty() || image.cols>16384 || image.rows>16384) throw std::invalid_argument("Image dimensions/decode");
    return image;
}
double fraction(const cv::Mat& image, const field_mask& field, bool core=false, cv::Point origin={}) {
    cv::Rect bounds=field.bounds; bounds.x-=origin.x; bounds.y-=origin.y;
    double value=cv::mean(image(bounds),core?field.core:field.mask)[0]/255;
    if (!std::isfinite(value) || value < -1e-12 || value > 1+1e-12) throw std::domain_error("Mask fraction");
    return std::clamp(value,0.0,1.0);
}
cv::Mat grow(const cv::Mat& seed, const cv::Mat& allowed) {
    cv::Mat result=seed.clone(), next;
    cv::Mat kernel=cv::getStructuringElement(cv::MORPH_ELLIPSE,{3,3});
    for (int i=0;i<12;++i) {
        cv::dilate(result,next,kernel); cv::bitwise_and(next,allowed,next); cv::bitwise_or(next,seed,next);
        if (cv::countNonZero(next)==cv::countNonZero(result)) break;
        next.copyTo(result);
    }
    return result;
}
}

int32_t anv_engine_create(const uint8_t* reference, uint64_t length, const anv_field* fields,
    int32_t count, const anv_point* points, int32_t point_count, void** handle) {
    if (!handle) return ANV_INVALID_INPUT;
    *handle=nullptr;
    if (!fields || !points || count<1 || count>1000 || point_count<3 || point_count>16000) return ANV_INVALID_INPUT;
    try {
        auto context=std::make_unique<engine>();
        context->reference=decode(reference,length);
        cv::resize(context->reference,context->half,{},0.5,0.5,cv::INTER_AREA);
        for (int i=0;i<count;++i) {
            const auto& input=fields[i];
            if (input.track<0 || input.track>2 || input.first_vertex<0 || input.vertex_count<3 ||
                input.vertex_count>16 || input.first_vertex>point_count-input.vertex_count) return ANV_INVALID_INPUT;
            std::vector<cv::Point> polygon;
            for (int j=0;j<input.vertex_count;++j) {
                const auto& p=points[input.first_vertex+j]; polygon.emplace_back(p.x,p.y);
            }
            cv::Rect bounds=cv::boundingRect(polygon);
            if (bounds.x<0 || bounds.y<0 || bounds.width<=0 || bounds.height<=0 ||
                bounds.x>context->half.cols-bounds.width || bounds.y>context->half.rows-bounds.height) return ANV_INVALID_INPUT;
            for (auto& p:polygon) p-=bounds.tl();
            cv::Mat mask=cv::Mat::zeros(bounds.size(),CV_8UC1), core;
            cv::fillPoly(mask,std::vector<std::vector<cv::Point>>{polygon},cv::Scalar(255));
            int erosion=std::max(1,int(std::min(bounds.width,bounds.height)*0.12));
            cv::Mat kernel=cv::getStructuringElement(cv::MORPH_ELLIPSE,{2*erosion+1,2*erosion+1});
            cv::erode(mask,core,kernel,{-1,-1},1,cv::BORDER_CONSTANT,cv::Scalar(0));
            context->fields.push_back({input.track,input.value,bounds,mask,core});
            context->scoring=i==0?bounds:context->scoring|bounds;
        }
        *handle=context.release(); return ANV_OK;
    } catch (const std::invalid_argument&) { return ANV_INVALID_INPUT; }
      catch (...) { return ANV_INTERNAL_ERROR; }
}
void anv_engine_destroy(void* handle) { delete static_cast<engine*>(handle); }

int32_t anv_engine_analyze(void* handle, const uint8_t* bytes, uint64_t length,
    anv_evidence* output, int32_t capacity, anv_analysis_result* result, uint8_t* optional_warp, uint64_t warp_capacity) {
    if (!result) return ANV_INVALID_INPUT;
    *result={};
    if (!handle || !output || capacity<0) return ANV_INVALID_INPUT;
    engine& context=*static_cast<engine*>(handle);
    if (size_t(capacity)<context.fields.size()*4) return ANV_BUFFER_TOO_SMALL;
    uint64_t required=uint64_t(context.half.cols)*context.half.rows*3;
    if (optional_warp && warp_capacity<required) return ANV_BUFFER_TOO_SMALL;
    try {
        cv::Mat photo=decode(bytes,length);
        anv_image reference{context.reference.data,uint64_t(context.reference.total()*3),context.reference.cols,context.reference.rows,int(context.reference.step),0};
        anv_image input{photo.data,uint64_t(photo.total()*3),photo.cols,photo.rows,int(photo.step),0};
        std::vector<uint8_t> buffer(size_t(required),0);
        anv_probe_result probe{};
        double spread=0;
        int status=probe_impl(&reference,&input,&probe,buffer.data(),required,&spread);
        if (status!=ANV_OK) return status;
        cv::Mat transform(3,3,CV_64F,probe.homography);
        double photo_scale=std::min(1.0,1600.0/std::max(photo.cols,photo.rows));
        cv::Size small_size(cvRound(photo.cols*photo_scale),cvRound(photo.rows*photo_scale));
        cv::Mat validity_source(small_size,CV_8UC1,cv::Scalar(255)), validity;
        cv::warpPerspective(validity_source,validity,transform,context.half.size(),cv::INTER_NEAREST);
        double visibility[3]={}, counts[3]={};
        for (const auto& f:context.fields) { visibility[f.track]+=fraction(validity,f); ++counts[f.track]; }
        for (int t=0;t<3;++t) { if (counts[t]==0) return ANV_INVALID_INPUT; visibility[t]/=counts[t]; }
        std::vector<cv::Point2f> corners{{0,0},{float(small_size.width),0},{float(small_size.width),float(small_size.height)},{0,float(small_size.height)}}, footprint;
        cv::perspectiveTransform(corners,footprint,transform);
        // Obtain the same inlier-target hull used by the reference registration.
        // Probe exports spread separately below; footprint is never a substitute for this gate.
        double scoring_area=double(context.scoring.width)*context.scoring.height;
        spread/=scoring_area;
        double minimum=*std::min_element(visibility,visibility+3);
        result->matches=probe.matches; result->inliers=probe.inliers;
        result->reprojection_p95=probe.reprojection_p95; result->minimum_visibility=minimum;
        result->spread=spread; result->width=context.half.cols; result->height=context.half.rows;
        std::copy(probe.homography,probe.homography+9,result->homography);
        bool finite=std::all_of(footprint.begin(),footprint.end(),[](auto p){return std::isfinite(p.x)&&std::isfinite(p.y);});
        if (!finite || !cv::isContourConvex(footprint) || std::abs(cv::contourArea(footprint))<scoring_area*0.8 || minimum<0.75 || spread<0.25)
            return ANV_REGISTRATION_FAILED;
        cv::Mat warped(context.half.rows,context.half.cols,CV_8UC3,buffer.data());
        cv::Mat current=warped(context.scoring), reference_roi=context.half(context.scoring), visible=validity(context.scoring);
        cv::Mat hsv,lab,reference_lab,light,reference_light,smooth,reference_smooth;
        cv::cvtColor(current,hsv,cv::COLOR_BGR2HSV); cv::cvtColor(current,lab,cv::COLOR_BGR2Lab);
        cv::cvtColor(reference_roi,reference_lab,cv::COLOR_BGR2Lab);
        cv::extractChannel(lab,light,0); cv::extractChannel(reference_lab,reference_light,0);
        cv::GaussianBlur(light,smooth,{},25); cv::GaussianBlur(reference_light,reference_smooth,{},25);
        cv::Mat difference=cv::Mat::zeros(hsv.size(),CV_8UC1);
        cv::Mat strict[4],loose[4];
        for (int c=0;c<4;++c) { strict[c]=cv::Mat::zeros(hsv.size(),CV_8UC1); loose[c]=cv::Mat::zeros(hsv.size(),CV_8UC1); }
        for (int y=0;y<hsv.rows;++y) for (int x=0;x<hsv.cols;++x) {
            if (!visible.at<uint8_t>(y,x)) continue;
            auto color=hsv.at<cv::Vec3b>(y,x), pixel=lab.at<cv::Vec3b>(y,x), empty=reference_lab.at<cv::Vec3b>(y,x);
            double delta=double(pixel[0])-empty[0]-smooth.at<uint8_t>(y,x)+reference_smooth.at<uint8_t>(y,x);
            double a=double(pixel[1])-empty[1], b=double(pixel[2])-empty[2];
            bool changed=std::sqrt(delta*delta+a*a+b*b)>24;
            if (!changed) continue;
            difference.at<uint8_t>(y,x)=255;
            int h=color[0],s=color[1],v=color[2];
            bool seeds[4]={(h<12||h>170)&&s>95&&v>55,h>=80&&h<=135&&s>70&&v>45,h>=18&&h<=40&&s>95&&v>90,v<85&&delta< -18};
            bool allowed[4]={(h<18||h>165)&&s>30&&v>25,h>=75&&h<=140&&s>30&&v>25,h>=15&&h<=45&&s>40&&v>45,v<110&&delta< -10};
            for (int c=0;c<4;++c) { if(seeds[c]) strict[c].at<uint8_t>(y,x)=255; if(allowed[c]) loose[c].at<uint8_t>(y,x)=255; }
        }
        std::vector<anv_evidence> evidence;
        for (int c=0;c<4;++c) {
            cv::Mat grown=grow(strict[c],loose[c]);
            for (const auto& field:context.fields) evidence.push_back({c,field.track,field.value,0,
                fraction(strict[c],field,false,context.scoring.tl()), fraction(strict[c],field,true,context.scoring.tl()),
                fraction(difference,field,false,context.scoring.tl()),fraction(difference,field,true,context.scoring.tl()),
                fraction(grown,field,false,context.scoring.tl()),fraction(validity,field),visibility[field.track]});
        }
        std::copy(evidence.begin(),evidence.end(),output);
        if (optional_warp) std::memcpy(optional_warp,buffer.data(),size_t(required));
        result->evidence_count=int32_t(evidence.size()); result->success=1;
        return ANV_OK;
    } catch (const std::invalid_argument&) { return ANV_INVALID_INPUT; }
      catch (...) { return ANV_INTERNAL_ERROR; }
}
