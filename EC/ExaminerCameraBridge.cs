using System.Text.Json;
using System.IO.MemoryMappedFiles;

namespace ETS2LA.EC;

public sealed class ExaminerCameraBridge : IDisposable
{
    private const string MapName = "Local\\ETS2LAECExaminerCamera";
    private readonly MemoryMappedFile _map;
    private readonly MemoryMappedViewAccessor _view;
    private readonly ExaminerCameraController _controller;
    private uint _sequence;

    public ExaminerCameraBridge(ExaminerCameraController controller)
    {
        _controller = controller;
        _map = MemoryMappedFile.CreateOrOpen(MapName, 64, MemoryMappedFileAccess.ReadWrite);
        _view = _map.CreateViewAccessor(0, 64, MemoryMappedFileAccess.ReadWrite);
    }

    public void Tick()
    {
        var target = _controller.Update();
        if (target is null) { Clear(); return; }
        _view.Write(0, 0x31434345u);
        _view.Write(4, 1u);
        _view.Write(8, 1u);
        _view.Write(12, (int)target.VehicleId);
        WriteVec(16, target.Position.X, target.Position.Y, target.Position.Z);
        _view.Write(28, target.CameraRotation.X); _view.Write(32, target.CameraRotation.Y);
        _view.Write(36, target.CameraRotation.Z); _view.Write(40, target.CameraRotation.W);
        WriteVec(44, target.LookTarget.X, target.LookTarget.Y, target.LookTarget.Z);
        _view.Write(56, 70f); _view.Write(60, ++_sequence); _view.Flush();
    }

    public void Clear()
    {
        _view.Write(0, 0x31434345u); _view.Write(4, 1u); _view.Write(8, 0u);
        _view.Write(12, -1); _view.Write(60, ++_sequence); _view.Flush();
    }

    private void WriteVec(long o, float x, float y, float z) { _view.Write(o,x); _view.Write(o+4,y); _view.Write(o+8,z); }
    public void Dispose() { _view.Dispose(); _map.Dispose(); }
}
