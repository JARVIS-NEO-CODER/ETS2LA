using System.Numerics;
using ETS2LA.Game.SDK;

namespace ETS2LA.EC;

/// <summary>
/// Pure C# side of the examiner camera. It consumes ETS2LA's existing
/// TrafficProvider and produces a camera target. No game memory is written here.
/// </summary>
public sealed class ExaminerCameraController
{
    public short? TargetVehicleId { get; private set; }
    public bool Active => TargetVehicleId.HasValue;

    // Local-space offset: behind the target and above it.
    public float Distance { get; set; } = 12f;
    public float Height { get; set; } = 5f;
    public float LookHeight { get; set; } = 1.8f;
    public float PositionSmoothing { get; set; } = 0.18f;

    private Vector3 _smoothedPosition;
    private bool _hasSmoothedPosition;

    public bool Select(short vehicleId)
    {
        var data = TrafficProvider.Current.GetCurrentTrafficData();
        if (data?.vehicles == null || !data.vehicles.Any(v => v.id == vehicleId))
            return false;

        TargetVehicleId = vehicleId;
        _hasSmoothedPosition = false;
        return true;
    }

    public void Clear()
    {
        TargetVehicleId = null;
        _hasSmoothedPosition = false;
    }

    public CameraTarget? Update()
    {
        if (!TargetVehicleId.HasValue)
            return null;

        var data = TrafficProvider.Current.GetCurrentTrafficData();
        var vehicle = data?.vehicles?.FirstOrDefault(v => v.id == TargetVehicleId.Value);
        if (vehicle == null)
        {
            Clear();
            return null;
        }

        var forward = Vector3.Transform(Vector3.UnitZ, vehicle.Rotation);
        var up = Vector3.Transform(Vector3.UnitY, vehicle.Rotation);
        if (forward.LengthSquared() < 0.001f) forward = Vector3.UnitZ;
        if (up.LengthSquared() < 0.001f) up = Vector3.UnitY;
        forward = Vector3.Normalize(forward);
        up = Vector3.Normalize(up);

        var desiredPosition = vehicle.Position - forward * Distance + up * Height;
        _smoothedPosition = !_hasSmoothedPosition
            ? desiredPosition
            : Vector3.Lerp(_smoothedPosition, desiredPosition, PositionSmoothing);
        _hasSmoothedPosition = true;

        var lookTarget = vehicle.Position + up * LookHeight;
        var lookRotation = LookAt(_smoothedPosition, lookTarget, up);

        return new CameraTarget
        {
            VehicleId = vehicle.id,
            Position = _smoothedPosition,
            VehicleRotation = vehicle.Rotation,
            LookTarget = lookTarget,
            CameraRotation = lookRotation
        };
    }

    private static Quaternion LookAt(Vector3 from, Vector3 to, Vector3 up)
    {
        var forward = Vector3.Normalize(to - from);
        if (forward.LengthSquared() < 0.0001f)
            return Quaternion.Identity;

        var right = Vector3.Normalize(Vector3.Cross(up, forward));
        var correctedUp = Vector3.Cross(forward, right);

        var matrix = new Matrix4x4(
            right.X, right.Y, right.Z, 0,
            correctedUp.X, correctedUp.Y, correctedUp.Z, 0,
            forward.X, forward.Y, forward.Z, 0,
            0, 0, 0, 1);
        return Quaternion.CreateFromRotationMatrix(matrix);
    }
}

public sealed class CameraTarget
{
    public short VehicleId { get; init; }
    public Vector3 Position { get; init; }
    public Quaternion VehicleRotation { get; init; }
    public Vector3 LookTarget { get; init; }
    public Quaternion CameraRotation { get; init; }
}
