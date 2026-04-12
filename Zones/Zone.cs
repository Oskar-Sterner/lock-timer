using System;
using System.Numerics;

namespace LockTimer.Zones;

public sealed record Zone(
    ZoneKind Kind,
    string Map,
    Vector3 Min,
    Vector3 Max,
    long UpdatedAtUnix)
{
    public bool Contains(Vector3 p) =>
        p.X >= Min.X && p.X <= Max.X &&
        p.Y >= Min.Y && p.Y <= Max.Y &&
        p.Z >= Min.Z && p.Z <= Max.Z;

    public bool IsZeroVolume =>
        Min.X == Max.X || Min.Y == Max.Y || Min.Z == Max.Z;

    public static Zone FromCorners(ZoneKind kind, string map, Vector3 a, Vector3 b, long updatedAtUnix) =>
        new(kind, map,
            Min: new Vector3(MathF.Min(a.X, b.X), MathF.Min(a.Y, b.Y), MathF.Min(a.Z, b.Z)),
            Max: new Vector3(MathF.Max(a.X, b.X), MathF.Max(a.Y, b.Y), MathF.Max(a.Z, b.Z)),
            UpdatedAtUnix: updatedAtUnix);
}
