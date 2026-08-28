#pragma once
#include <cstdint>

namespace ec {
#pragma pack(push, 1)
struct vec3 { float x, y, z; };
struct quat { float x, y, z, w; };
struct camera_command {
    uint32_t magic;
    uint32_t version;
    uint32_t active;
    int32_t target_vehicle_id;
    vec3 position;
    quat rotation;
    vec3 look_target;
    float fov;
    uint32_t sequence;
};
#pragma pack(pop)

constexpr uint32_t CAMERA_COMMAND_MAGIC = 0x31434345; // "ECC1"
constexpr uint32_t CAMERA_COMMAND_VERSION = 1;
static_assert(sizeof(camera_command) == 64, "EC camera command ABI changed");
}
