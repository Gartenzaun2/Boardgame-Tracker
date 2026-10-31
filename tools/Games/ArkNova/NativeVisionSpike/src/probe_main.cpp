#include "arknova_vision.h"
#include <fstream>
#include <iostream>
#include <iterator>
#include <vector>
#include <cstring>
#include <iomanip>

struct image_file {
    std::vector<uint8_t> bytes;
    anv_image image{};
    bool read(const char* path) {
        std::ifstream stream(path, std::ios::binary);
        bytes.assign(std::istreambuf_iterator<char>(stream), {});
        if (bytes.size() < 16 || std::memcmp(bytes.data(), "ANB1", 4)) return false;
        std::memcpy(&image.width, bytes.data() + 4, 4);
        std::memcpy(&image.height, bytes.data() + 8, 4);
        std::memcpy(&image.stride, bytes.data() + 12, 4);
        image.data = bytes.data() + 16;
        image.length = bytes.size() - 16;
        return true;
    }
};

int main(int argc, char** argv) {
    if (argc == 3 && (std::strcmp(argv[1], "--score") == 0 || std::strcmp(argv[1], "--engine-self-test") == 0)) {
        const std::string root=argv[2];
        auto read=[&](std::string name) { std::ifstream stream(root+"/"+name,std::ios::binary); return std::vector<uint8_t>(std::istreambuf_iterator<char>(stream),{}); };
        auto reference=read("reference.png"), geometry=read("fields.bin");
        if (geometry.size()<8) return 2;
        int32_t field_count,point_count;
        std::memcpy(&field_count,geometry.data(),4); std::memcpy(&point_count,geometry.data()+4,4);
        if (field_count<1 || point_count<1 || geometry.size()!=8+size_t(field_count)*sizeof(anv_field)+size_t(point_count)*sizeof(anv_point)) return 2;
        void* handle=nullptr;
        int status=anv_engine_create(reference.data(),reference.size(),reinterpret_cast<const anv_field*>(geometry.data()+8),field_count,
            reinterpret_cast<const anv_point*>(geometry.data()+8+size_t(field_count)*sizeof(anv_field)),point_count,&handle);
        if(status) { std::cerr<<"create="<<status<<"\n"; return status; }
        if (std::strcmp(argv[1], "--engine-self-test") == 0) {
            auto blank=read("blank.png");
            std::vector<anv_evidence> fields(size_t(field_count)*4);
            fields[0].full=123;
            anv_analysis_result result{};
            uint8_t warp=123;
            if(anv_engine_analyze(handle,blank.data(),blank.size(),fields.data(),0,&result,nullptr,0)!=ANV_BUFFER_TOO_SMALL) return 11;
            if(anv_engine_analyze(handle,blank.data(),blank.size(),fields.data(),int(fields.size()),&result,&warp,1)!=ANV_BUFFER_TOO_SMALL || warp!=123) return 12;
            if(anv_engine_analyze(handle,nullptr,0,fields.data(),int(fields.size()),&result,nullptr,0)!=ANV_INVALID_INPUT) return 13;
            if(anv_engine_analyze(handle,blank.data(),blank.size(),fields.data(),int(fields.size()),&result,nullptr,0)!=ANV_REGISTRATION_FAILED) return 14;
            if(result.success || result.evidence_count || fields[0].full!=123) return 15;
            if(anv_engine_analyze(handle,blank.data(),blank.size(),fields.data(),int(fields.size()),nullptr,nullptr,0)!=ANV_INVALID_INPUT) return 16;
            anv_engine_destroy(handle);
            anv_engine_destroy(nullptr);
            std::cout<<"Engine ABI checks passed: short evidence, short warp, invalid decode, blank registration, untouched failure output, null result, null destroy\n";
            return 0;
        }
        std::ifstream inputs(root+"/inputs.txt");
        std::ofstream report(root+"/native-results.jsonl");
        std::string id;
        while(std::getline(inputs,id)) {
            if (!id.empty() && id.back()=='\r') id.pop_back();
            if (id.empty()) continue;
            auto image=read(id+".png");
            std::vector<anv_evidence> fields(size_t(field_count)*4);
            std::vector<uint8_t> warp(2000*1400*3);
            anv_analysis_result result{};
            status=anv_engine_analyze(handle,image.data(),image.size(),fields.data(),int(fields.size()),&result,warp.data(),warp.size());
            double mae=0; int maximum=0; auto golden=read(id+".warp.bgr");
            if(status==0 && golden.size()==warp.size()) {
                uint64_t sum=0; for(size_t i=0;i<warp.size();++i) { int delta=std::abs(int(golden[i])-int(warp[i])); sum+=delta; maximum=std::max(maximum,delta); }
                mae=double(sum)/warp.size();
            }
            report<<std::setprecision(17)<<"{\"id\":\""<<id<<"\",\"status\":"<<status<<",\"matches\":"<<result.matches<<",\"inliers\":"<<result.inliers
                <<",\"p95\":"<<result.reprojection_p95<<",\"visibility\":"<<result.minimum_visibility<<",\"spread\":"<<result.spread
                <<",\"warpMae\":"<<mae<<",\"warpMax\":"<<maximum<<",\"homography\":[";
            for(int i=0;i<9;++i) report<<(i?",":"")<<result.homography[i];
            report<<"],\"fields\":[";
            for(int i=0;i<result.evidence_count;++i) {
                const auto& f=fields[i];
                report<<(i?",":"")<<"["<<f.color<<","<<f.track<<","<<f.value<<","<<f.full<<","<<f.core<<","<<f.difference<<","<<f.core_difference
                    <<","<<f.grown<<","<<f.visibility<<","<<f.track_visibility<<"]";
            }
            report<<"]}\n"; report.flush();
            std::cout<<id<<" status="<<status<<" matches="<<result.matches<<"\n";
        }
        auto blank=read("blank.png"); std::vector<anv_evidence> fields(size_t(field_count)*4); anv_analysis_result result{};
        if(anv_engine_analyze(handle,blank.data(),blank.size(),fields.data(),int(fields.size()),&result,nullptr,0)!=ANV_REGISTRATION_FAILED) return 9;
        if(anv_engine_create(nullptr,0,nullptr,0,nullptr,0,nullptr)!=ANV_INVALID_INPUT) return 10;
        anv_engine_destroy(handle);
        return 0;
    }
    if (argc == 2 && std::strcmp(argv[1], "--self-test") == 0) {
        anv_probe_result result{};
        uint8_t byte = 0;
        if (anv_abi_version() != 1 || anv_probe(nullptr, nullptr, &result, &byte, 1) != ANV_INVALID_INPUT)
            return 1;
        anv_image invalid{ &byte, 1, 1, 1, 3, 0 };
        if (anv_probe(&invalid, &invalid, &result, &byte, 1) != ANV_INVALID_INPUT)
            return 1;
        std::vector<uint8_t> pixels(64 * 64 * 3, 0);
        anv_image blank{ pixels.data(), pixels.size(), 64, 64, 64 * 3, 0 };
        byte = 123;
        if (anv_probe(&blank, &blank, &result, &byte, 1) != ANV_BUFFER_TOO_SMALL || byte != 123)
            return 1;
        std::vector<uint8_t> output(32 * 32 * 3, 123);
        if (anv_probe(&blank, &blank, &result, output.data(), output.size()) != ANV_REGISTRATION_FAILED)
            return 1;
        if (result.matches || result.inliers || result.warp_bytes || output[0] != 123)
            return 1;
        if (anv_probe(&blank, &blank, nullptr, output.data(), output.size()) != ANV_INVALID_INPUT)
            return 1;
        std::cout << "ABI checks passed: null input, short input, short output, blank registration, null result, untouched failure output\n";
        return 0;
    }
    if (argc != 4) {
        std::cerr << "Usage: arknova_probe reference.bgr photo.bgr warp.bgr | --self-test\n";
        return 2;
    }
    image_file reference, photo;
    if (!reference.read(argv[1]) || !photo.read(argv[2])) return 2;
    if (reference.image.width <= 0 || reference.image.height <= 0 ||
        reference.image.width > 16384 || reference.image.height > 16384) return 2;
    // At most the original reference size; native code validates the exact need.
    std::vector<uint8_t> warp(size_t(reference.image.width) * reference.image.height * 3);
    anv_probe_result result{};
    const int status = anv_probe(&reference.image, &photo.image, &result, warp.data(), warp.size());
    std::cout << std::setprecision(17) << "{\"status\":" << status
        << ",\"matches\":" << result.matches << ",\"inliers\":" << result.inliers
        << ",\"reprojectionP95\":" << result.reprojection_p95 << ",\"homography\":[";
    for (int i = 0; i < 9; ++i) std::cout << (i ? "," : "") << result.homography[i];
    std::cout << "]}\n";
    if (status != ANV_OK) return status;
    std::ofstream output(argv[3], std::ios::binary);
    output.write(reinterpret_cast<const char*>(warp.data()), std::streamsize(result.warp_bytes));
    return output ? 0 : 2;
}
