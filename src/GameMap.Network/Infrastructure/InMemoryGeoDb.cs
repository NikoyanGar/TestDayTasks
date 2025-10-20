using GameMap.Core.Storage;
using System.Collections.Concurrent;

namespace GameMap.Network.Infrastructure;

/// <summary>
/// In-memory implementation of IGeoDb for development/testing.
/// Distances are approximated on a plane using degrees converted by LinearCoordinateConverter.
/// </summary>
public sealed class InMemoryGeoDb : IGeoDb
{
    private readonly ConcurrentDictionary<string, ConcurrentDictionary<string, (double lon, double lat)>> _geo = new();
    private readonly ConcurrentDictionary<string, string> _strings = new();

    public void GeoAdd(string key, double lon, double lat, string member)
    {
        var set = _geo.GetOrAdd(key, _ => new());
        set[member] = (lon, lat);
    }

    public IEnumerable<string> GeoRadius(string key, double lon, double lat, double radiusKm)
    {
        if (!_geo.TryGetValue(key, out var set)) yield break;

        const double KmPerDegree = 111.0; // same scale as LinearCoordinateConverter
        foreach (var kv in set)
        {
            var (mlon, mlat) = kv.Value;
            var dxKm = (mlon - lon) * KmPerDegree;
            var dyKm = (mlat - lat) * KmPerDegree;
            var distKm = Math.Sqrt(dxKm * dxKm + dyKm * dyKm);
            if (distKm <= radiusKm)
                yield return kv.Key;
        }
    }

    public void StringSet(string key, string value) => _strings[key] = value;

    public string? StringGet(string key) => _strings.TryGetValue(key, out var v) ? v : null;

    public void KeyDelete(string key)
    {
        _strings.TryRemove(key, out _);
        _geo.TryRemove(key, out _);
    }

    public void SortedSetRemove(string key, string member)
    {
        if (_geo.TryGetValue(key, out var set))
        {
            set.TryRemove(member, out _);
        }
    }
}
