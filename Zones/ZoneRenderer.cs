using System.Drawing;
using System.Numerics;
using DeadworksManaged.Api;

namespace LockTimer.Zones;

public sealed class ZoneRenderer
{
    // Each text entity is a segment of repeated blocks covering this many world units.
    private const float SegmentLength = 80f;
    // Size of each block character in world units.
    private const float BlockScale = 0.15f;
    // Approximate pixel width of █ at fontSize 100.
    private const float CharPixelWidth = 55f;

    private readonly List<CBaseEntity> _spawned = new();

    public void Render(Zone zone)
    {
        var color = zone.Kind == ZoneKind.Start ? Color.LimeGreen : Color.Red;

        // Pre-compute segment text
        float worldPerChar = BlockScale * CharPixelWidth;
        int charsPerSegment = Math.Max(1, (int)(SegmentLength / worldPerChar));
        string segmentText = new string('\u2588', charsPerSegment);

        foreach (var (a, b) in Edges(zone.Min, zone.Max))
        {
            float length = Vector3.Distance(a, b);
            var dir = b - a;
            bool isVertical = MathF.Abs(dir.Z) > MathF.Abs(dir.X) + MathF.Abs(dir.Y);

            // Compute yaw so text aligns along the edge direction.
            // For vertical edges, also pitch 90 to stand upright.
            float yaw = MathF.Atan2(dir.Y, dir.X) * (180f / MathF.PI);
            var angles = isVertical
                ? new Vector3(90f, yaw, 0f)
                : new Vector3(0f, yaw, 0f);

            // Place segments from corner to corner. Skip the first and last
            // half-segment so the line doesn't overshoot the corners.
            float halfSeg = SegmentLength / 2f;
            int segments = Math.Max(1, (int)MathF.Round((length - SegmentLength) / SegmentLength)) + 1;
            for (int i = 0; i < segments; i++)
            {
                float dist = halfSeg + i * ((length - SegmentLength) / Math.Max(1, segments - 1));
                if (segments == 1) dist = length / 2f;
                float t = dist / length;
                var point = Vector3.Lerp(a, b, t);
                SpawnText(segmentText, point, color, BlockScale, angles);
            }
        }
    }

    private void SpawnText(string text, Vector3 position, Color color, float worldUnitsPerPx, Vector3 angles)
    {
        var wt = CPointWorldText.Create(
            message: text,
            position: position,
            fontSize: 100f,
            worldUnitsPerPx: worldUnitsPerPx,
            r: color.R, g: color.G, b: color.B, a: color.A,
            reorientMode: 0);
        if (wt is not null)
        {
            wt.Fullbright = true;
            wt.DepthOffset = 0.1f;
            wt.JustifyHorizontal = HorizontalJustify.Center;
            wt.JustifyVertical = VerticalJustify.Center;
            wt.Teleport(angles: angles);
            _spawned.Add(wt);
        }
    }

    public void ClearAll()
    {
        foreach (var e in _spawned)
        {
            try { e.Remove(); } catch { }
        }
        _spawned.Clear();
    }

    private static IEnumerable<(Vector3 A, Vector3 B)> Edges(Vector3 min, Vector3 max)
    {
        var c000 = new Vector3(min.X, min.Y, min.Z);
        var c100 = new Vector3(max.X, min.Y, min.Z);
        var c010 = new Vector3(min.X, max.Y, min.Z);
        var c110 = new Vector3(max.X, max.Y, min.Z);
        var c001 = new Vector3(min.X, min.Y, max.Z);
        var c101 = new Vector3(max.X, min.Y, max.Z);
        var c011 = new Vector3(min.X, max.Y, max.Z);
        var c111 = new Vector3(max.X, max.Y, max.Z);

        // Bottom face
        yield return (c000, c100);
        yield return (c100, c110);
        yield return (c110, c010);
        yield return (c010, c000);
        // Top face
        yield return (c001, c101);
        yield return (c101, c111);
        yield return (c111, c011);
        yield return (c011, c001);
        // Vertical edges
        yield return (c000, c001);
        yield return (c100, c101);
        yield return (c010, c011);
        yield return (c110, c111);
    }
}
