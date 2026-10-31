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

/* Coordinates are the existing C# Math.Round(polygon * 0.5) analysis pixels.
   Track: 0 attractiveness, 1 conservation, 2 reputation. Color: red/blue/yellow/black. */
typedef struct anv_point { int32_t x, y; } anv_point;
typedef struct anv_field { int32_t track, value, first_vertex, vertex_count; } anv_field;
typedef struct anv_evidence {
    int32_t color, track, value, reserved;
    double full, core, difference, core_difference, grown, visibility, track_visibility;
} anv_evidence;
typedef struct anv_analysis_result {
    int32_t success, matches, inliers, width, height, evidence_count;
    double reprojection_p95, minimum_visibility, spread;
    double homography[9];
} anv_analysis_result;

/* Handle owns reference/masks; input bytes are borrowed only for each call.
   Encoded input is JPEG/PNG decoded BGR8 with EXIF orientation by OpenCV.
   A handle is serialized by the managed owner; destroy only after calls finish. */
ANV_API int32_t anv_engine_create(const uint8_t* reference, uint64_t reference_length,
    const anv_field* fields, int32_t field_count, const anv_point* points, int32_t point_count, void** handle);
ANV_API void anv_engine_destroy(void* handle);
ANV_API int32_t anv_engine_analyze(void* handle, const uint8_t* photo, uint64_t photo_length,
    anv_evidence* evidence, int32_t capacity, anv_analysis_result* result,
    uint8_t* optional_warp, uint64_t warp_capacity);
#ifdef __cplusplus
}
#endif
