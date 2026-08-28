using System.IO.MemoryMappedFiles;
using System.Numerics;

namespace ETS2LA.Game.SDK;

/// <summary>
/// Shared-memory command channel for the native ETS2LA game plugin.
/// The C# side computes the examiner camera transform; the native plugin
/// can consume it and write the actual in-game camera placement.
/// Layout (64 bytes): magic, version, active, target id, position, rotation,
/// look-at, FOV and sequence.
/// </summary>
public sealed class ECExaminerCameraCommand : IDisposable
{
    private const string MapName = "Local\\ETS2LAECExaminerCamera";
    private const int MapSize = 64;
    private const uint Magic = 0x31434345; // "ECC1" little-endian
    private const uint Version = 1;

    private readonly MemoryMappedFile _map;
    private readonly MemoryMappedViewAccessor _view;
    private uint _sequence;

    public ECExaminerCameraCommand()
    {
        _map = MemoryMappedFile.CreateOrOpen(MapName, MapSize, MemoryMappedFileAccess.ReadWrite);
        _view = _map.CreateViewAccessor(0, MapSize, MemoryMappedFileAccess.ReadWrite);
    }

    public void Publish(CameraTarget target, bool active)
    {
        _view.Write(0, Magic);
        _view.Write(4, Version);
        _view.Write(8, active ? 1u : 0u);
        _view.Write(12, (int)target.VehicleId);
        WriteVector3(16, target.Position);
        WriteQuaternion(28, target.VehicleRotation);
        WriteVector3(44, target.LookTarget);
        _view.Write(56, 70f);
        _view.Write(60, unchecked(++_sequence));
        _view.Flush();
    }

    public void Clear()
    {
        _view.Write(0, Magic);
        _view.Write(4, Version);
        _view.Write(8, 0u);
        _view.Write(12, -1);
        _view.Write(60, unchecked(++_sequence));
        _view.Flush();
    }

    private void WriteVector3(long offset, Vector3 value)
    {
        _view.Write(offset, value.X);
        _view.Write(offset + 4, value.Y);
        _view.Write(offset + 8, value.Z);
    }

    private void WriteQuaternion(long offset, Quaternion value)
    {
        _view.Write(offset, value.X);
        _view.Write(offset + 4, value.Y);
        _view.Write(offset + 8, value.Z);
        _view.Write(offset + 12, value.W);
    }

    public void Dispose()
    {
        _view.Dispose();
        _map.Dispose();
    }
}
