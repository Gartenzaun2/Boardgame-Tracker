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
