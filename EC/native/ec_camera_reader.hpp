#pragma once

#include <cstdint>
#include "EC/camera_command.h"

namespace ec {

class CameraReader {
public:
    bool read(camera_command& out);
    void clear();

private:
    uint32_t last_sequence_ = 0;
};

} // namespace ec
