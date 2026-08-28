# EC native camera bridge

This directory contains the native-side contract for the examiner camera.

The bridge consumes the 64-byte `Local\\ETS2LAECExaminerCamera` command block emitted by `ECExaminerCameraCommand`.

Before writing `core_camera_u.placement`, the native plugin must validate the magic/version/active flag and consume only complete command sequences. The camera write should run from the existing frame-end/tick path so it is synchronized with the plugin's camera lifecycle.

Target: ETS2 1.60.
