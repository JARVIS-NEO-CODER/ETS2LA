using System.Numerics;
using ETS2LA.Logging;

namespace ETS2LA.Game.SDK;

/// <summary>
/// First EC (Examiner Connect) camera prototype.
/// Reuses ETS2LA's existing Convoy-player vehicle stream and computes a
/// smooth chase-camera transform relative to a selected vehicle.
/// This stage does not write to the game's camera yet.
/// </summary>
public sealed class ECExaminerCamera
{
    private static readonly Lazy<ECExaminerCamera> _instance = new(() => new ECExaminerCamera());
    public static ECExaminerCamera Current => _instance.Value;

    private readonly object _sync = new();
    private short? _targetId;
    private CameraTarget _current = CameraTarget.Empty;
    private bool _running;

    public float FollowDistance { get; set; } = 14f;
    public float FollowHeight { get; set; } = 4f;
    public float Smoothing { get; set; } = 10f;

    public CameraTarget CurrentTarget
    {
        get { lock (_sync) return _current; }
    }

    public bool IsFollowing => _targetId.HasValue;

    private ECExaminerCamera() { }

    public void Start()
    {
        if (_running) return;
        _running = true;
        _ = Task.Run(UpdateLoop);
        Logger.Info("EC examiner camera prototype started.");
    }

    public void Stop()
    {
        _running = false;
        lock (_sync)
        {
            _targetId = null;
            _current = CameraTarget.Empty;
        }
    }

    public void Follow(short vehicleId)
    {
        lock (_sync) _targetId = vehicleId;
        Logger.Info($"EC examiner camera target selected: vehicle {vehicleId}.");
    }

    public void ClearTarget()
    {
        lock (_sync)
        {
            _targetId = null;
            _current = CameraTarget.Empty;
        }
    }

    private async Task UpdateLoop()
    {
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        long previousTicks = stopwatch.ElapsedTicks;

        while (_running)
        {
            long nowTicks = stopwatch.ElapsedTicks;
            float dt = (float)((nowTicks - previousTicks) / (double)System.Diagnostics.Stopwatch.Frequency);
            previousTicks = nowTicks;
            dt = Math.Clamp(dt, 0.001f, 0.1f);

            short? targetId;
            lock (_sync) targetId = _targetId;

            if (targetId.HasValue)
            {
                var traffic = TrafficProvider.Current.GetCurrentTrafficData();
                var target = traffic?.vehicles.FirstOrDefault(v => v.id == targetId.Value && !v.isTrailer);

                if (target != null)
                {
                    var desired = BuildTarget(target);
                    lock (_sync) _current = Smooth(_current, desired, dt);
                }
            }

            await Task.Delay(16).ConfigureAwait(false);
        }
    }

    private CameraTarget BuildTarget(TrafficVehicle vehicle)
    {
        var localOffset = new Vector3(0f, FollowHeight, -FollowDistance);
        var cameraPosition = vehicle.Position + Vector3.Transform(localOffset, vehicle.Rotation);
        var lookTarget = vehicle.Position + Vector3.Transform(new Vector3(0f, 2f, 0f), vehicle.Rotation);
        var forward = Vector3.Normalize(lookTarget - cameraPosition);

        return new CameraTarget(true, vehicle.id, cameraPosition, vehicle.Rotation, lookTarget, forward, vehicle.speed);
    }

    private CameraTarget Smooth(CameraTarget current, CameraTarget desired, float dt)
    {
        if (!current.Valid || current.VehicleId != desired.VehicleId) return desired;

        float amount = 1f - MathF.Exp(-Smoothing * dt);
        var position = Vector3.Lerp(current.Position, desired.Position, amount);
        var lookTarget = Vector3.Lerp(current.LookTarget, desired.LookTarget, amount);
        var forward = Vector3.Normalize(lookTarget - position);
        var rotation = Quaternion.Slerp(current.VehicleRotation, desired.VehicleRotation, amount);

        return new CameraTarget(true, desired.VehicleId, position, rotation, lookTarget, forward, desired.Speed);
    }
}

public readonly record struct CameraTarget(
    bool Valid,
    short VehicleId,
    Vector3 Position,
    Quaternion VehicleRotation,
    Vector3 LookTarget,
    Vector3 Forward,
    float Speed
)
{
    public static CameraTarget Empty => new(false, -1, Vector3.Zero, Quaternion.Identity, Vector3.Zero, Vector3.UnitZ, 0f);
}
