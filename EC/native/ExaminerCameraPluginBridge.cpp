// ETS2 1.60 examiner-camera native bridge.
// Consumes the EC shared-memory command and applies placement_t to the active camera.

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

static constexpr uint32_t MAGIC = 0x31434345;
static HANDLE map = nullptr;
static void* view = nullptr;
static uint32_t last_sequence = 0;
static prism::placement_t saved_placement{};
static bool saved = false;

static bool open_map() {
    if (view) return true;
    map = OpenFileMappingA(FILE_MAP_READ, FALSE, "Local\\ETS2LAECExaminerCamera");
    if (!map) return false;
    view = MapViewOfFile(map, FILE_MAP_READ, 0, 0, sizeof(Command));
    if (!view) { CloseHandle(map); map = nullptr; return false; }
    return true;
}

static bool read_command(Command& out) {
    if (!open_map()) return false;
    Command snapshot{};
    std::memcpy(&snapshot, view, sizeof(snapshot));
    if (snapshot.magic != MAGIC || snapshot.version != 1) return false;
    if (snapshot.sequence == last_sequence) return false;
    out = snapshot;
    last_sequence = snapshot.sequence;
    return true;
}

static void restore(prism::core_camera_u* camera) {
    if (!camera || !saved) return;
    camera->placement = saved_placement;
    saved = false;
}

void tick() {
    Command c{};
    if (!read_command(c)) return;

    auto* manager = prism::camera_manager_u::get();
    if (!manager) return;
    const uint32_t index = manager->current_camera;
    if (index >= manager->cameras.size()) return;
    auto* camera = manager->cameras[index];
    if (!camera) return;

    if (!c.active || c.target_vehicle_id < 0) {
        restore(camera);
        return;
    }

    if (!saved) {
        saved_placement = camera->placement;
        saved = true;
    }

    camera->placement.pos.x = c.position.x;
    camera->placement.pos.y = c.position.y;
    camera->placement.pos.z = c.position.z;
    camera->placement.rot.w = c.rotation.w;
    camera->placement.rot.x = c.rotation.x;
    camera->placement.rot.y = c.rotation.y;
    camera->placement.rot.z = c.rotation.z;

    // cx/cz are the compact cell coordinates. Keep them unchanged because
    // the controller already supplies world-space position for the camera.
}

void shutdown() {
    if (view) { UnmapViewOfFile(view); view = nullptr; }
    if (map) { CloseHandle(map); map = nullptr; }
    saved = false;
}
}
