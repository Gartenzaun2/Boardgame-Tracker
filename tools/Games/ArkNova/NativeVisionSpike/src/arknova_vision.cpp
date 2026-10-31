#include "arknova_vision.h"
#include <opencv2/core.hpp>
#include <opencv2/imgproc.hpp>
#include <opencv2/features2d.hpp>
#include <opencv2/calib3d.hpp>
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

int32_t anv_probe(const anv_image* reference, const anv_image* photo,
                  anv_probe_result* result, uint8_t* warp, uint64_t capacity) {
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
        auto sift = cv::SIFT::create(6000);
        std::vector<cv::KeyPoint> reference_points, photo_points;
        cv::Mat reference_descriptors, photo_descriptors;
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
