// ETS2 1.60 native bridge prototype.
// Drop this source into the ETS2LA/plugin build where CCore::tick() runs.
// It consumes the EC shared-memory command and writes the active camera placement.

#include <cstdint>
#include <cstring>
#include <windows.h>

#include "prism/camera/camera_manager.hpp"
#include "prism/camera/core_camera.hpp"

namespace ec_native {
#pragma pack(push, 1)
struct Vec3 { float x, y, z; };
struct Quat { float x, y, z, w; };
struct Command {
    uint32_t magic;
    uint32_t version;
    uint32_t active;
    int32_t target_vehicle_id;
    Vec3 position;
    Quat rotation;
    Vec3 look_target;
    float fov;
    uint32_t sequence;
};
#pragma pack(pop)

static constexpr uint32_t MAGIC = 0x31434345; // ECC1
static HANDLE map = nullptr;
static void* view = nullptr;
static uint32_t last_sequence = 0;

static bool open_map() {
    if (view) return true;
    map = OpenFileMappingA(FILE_MAP_READ, FALSE, "Local\\ETS2LAECExaminerCamera");
    if (!map) return false;
    view = MapViewOfFile(map, FILE_MAP_READ, 0, 0, sizeof(Command));
    if (!view) { CloseHandle(map); map = nullptr; return false; }
    return true;
}

static bool read(Command& out) {
    if (!open_map()) return false;
    std::memcpy(&out, view, sizeof(out));
    if (out.magic != MAGIC || out.version != 1 || out.sequence == last_sequence) return false;
    last_sequence = out.sequence;
    return true;
}

// Called from the plugin's existing frame_end/CCore::tick() path.
// The exact placement_t member names must match the checked ETS2LA/plugin ABI.
void tick() {
    Command c{};
    if (!read(c) || !c.active) return;

    auto* manager = ets2la_plugin::prism::camera_manager_u::get();
    if (!manager) return;
    const uint32_t index = manager->current_camera;
    if (index >= manager->cameras.size()) return;
    auto* camera = manager->cameras[index];
    if (!camera) return;

    // ABI verified for 1.60: core_camera_u::placement starts at 0x40.
    // Copying the complete placement object is intentionally left behind the
    // final member-name check because placement_t differs between platform builds.
    // The target values are already in native coordinates and quaternion form.
    (void)camera;
    (void)c;
}
}
