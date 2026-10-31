#pragma once
#include <stdint.h>

#if defined(_WIN32)
#define ANV_API __declspec(dllexport)
#else
#define ANV_API __attribute__((visibility("default")))
#endif
#ifdef __cplusplus
extern "C" {
#endif

enum anv_status {
    ANV_OK = 0, ANV_INVALID_INPUT = 1, ANV_BUFFER_TOO_SMALL = 2,
    ANV_REGISTRATION_FAILED = 3, ANV_INTERNAL_ERROR = 4
};

/* Borrowed, normalized BGR8; top-left origin, positive stride, no EXIF.
   Caller owns all buffers, keeps them alive for the synchronous call, and
   provides their exact lengths. Native code never retains or frees them. */
typedef struct anv_image {
    const uint8_t* data;
    uint64_t length;
    int32_t width;
    int32_t height;
    int32_t stride;
    int32_t reserved;
} anv_image;

typedef struct anv_probe_result {
    int32_t matches;
    int32_t inliers;
    int32_t warp_width;
    int32_t warp_height;
    double reprojection_p95;
    /* Row-major registration-to-half-master transform, not camera pose. */
    double homography[9];
    uint64_t warp_bytes;
} anv_probe_result;

ANV_API int32_t anv_abi_version(void);

/* Registration capability probe, NOT a scoring engine or board acceptance
   decision. No masks/visibility/spread gate yet. On failure result is zero and
   warp must be ignored. No exception crosses this ABI. Calls are independent. */
ANV_API int32_t anv_probe(const anv_image* reference, const anv_image* photo,
                         anv_probe_result* result, uint8_t* warp, uint64_t capacity);
#ifdef __cplusplus
}
#endif
