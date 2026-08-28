using ETS2LA.Game.SDK;

namespace ETS2LA.EC;

/// <summary>Small, UI-agnostic selection model for the examiner camera.</summary>
public sealed class ExaminerCameraSelector
{
    private readonly ExaminerCameraController _camera;

    public ExaminerCameraSelector(ExaminerCameraController camera) => _camera = camera;

    public IReadOnlyList<TargetOption> GetPlayers()
    {
        var data = TrafficProvider.Current.GetCurrentTrafficData();
        if (data?.vehicles == null) return Array.Empty<TargetOption>();

        return data.vehicles
            .Where(v => !v.isTrailer && v.id >= 0)
            .Select(v => new TargetOption(v.id, $"Joueur #{v.id}", v.speed))
            .ToList();
    }

    public bool Select(short vehicleId) => _camera.Select(vehicleId);
    public void Stop() => _camera.Clear();
}

public readonly record struct TargetOption(short Id, string Label, float Speed);
