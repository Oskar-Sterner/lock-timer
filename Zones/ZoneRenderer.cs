using System.Drawing;
using System.Numerics;
using DeadworksManaged.Api;

namespace LockTimer.Zones;

public sealed class ZoneRenderer
{
    // Static marker particle. See Task 4.2 notes for swap candidates if this
    // doesn't render visibly on first in-game load.
    private const string MarkerParticle = "particles/ui_mouseactions/ping_world.vpcf";

    private readonly Dictionary<ZoneKind, List<CParticleSystem>> _spawned = new();

    public void Render(Zone zone)
    {
        Clear(zone.Kind);

        var color = zone.Kind == ZoneKind.Start ? Color.LimeGreen : Color.Red;
        var markers = new List<CParticleSystem>(20);

        foreach (var point in OutlinePoints(zone.Min, zone.Max))
        {
            var p = CParticleSystem
                .Create(MarkerParticle)
                .AtPosition(point)
                .WithTint(color, tintCP: 0)
                .StartActive(true)
                .Spawn();
            if (p is not null) markers.Add(p);
        }

        _spawned[zone.Kind] = markers;
    }

    public void Clear(ZoneKind kind)
    {
        if (!_spawned.TryGetValue(kind, out var list)) return;
        foreach (var p in list) p.Destroy();
        list.Clear();
        _spawned.Remove(kind);
    }

    public void ClearAll()
    {
        foreach (var list in _spawned.Values)
            foreach (var p in list) p.Destroy();
        _spawned.Clear();
    }

    private static IEnumerable<Vector3> OutlinePoints(Vector3 min, Vector3 max)
    {
        // 8 corners
        var c000 = new Vector3(min.X, min.Y, min.Z);
        var c100 = new Vector3(max.X, min.Y, min.Z);
        var c010 = new Vector3(min.X, max.Y, min.Z);
        var c110 = new Vector3(max.X, max.Y, min.Z);
        var c001 = new Vector3(min.X, min.Y, max.Z);
        var c101 = new Vector3(max.X, min.Y, max.Z);
        var c011 = new Vector3(min.X, max.Y, max.Z);
        var c111 = new Vector3(max.X, max.Y, max.Z);

        yield return c000; yield return c100; yield return c010; yield return c110;
        yield return c001; yield return c101; yield return c011; yield return c111;

        // 12 edge midpoints — makes the outline readable even on large zones
        yield return Vector3.Lerp(c000, c100, 0.5f);
        yield return Vector3.Lerp(c100, c110, 0.5f);
        yield return Vector3.Lerp(c110, c010, 0.5f);
        yield return Vector3.Lerp(c010, c000, 0.5f);
        yield return Vector3.Lerp(c001, c101, 0.5f);
        yield return Vector3.Lerp(c101, c111, 0.5f);
        yield return Vector3.Lerp(c111, c011, 0.5f);
        yield return Vector3.Lerp(c011, c001, 0.5f);
        yield return Vector3.Lerp(c000, c001, 0.5f);
        yield return Vector3.Lerp(c100, c101, 0.5f);
        yield return Vector3.Lerp(c110, c111, 0.5f);
        yield return Vector3.Lerp(c010, c011, 0.5f);
    }
}
