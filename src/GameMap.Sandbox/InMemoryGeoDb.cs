using GameMap.Core.Storage;
using System.Collections.Concurrent;

sealed class InMemoryGeoDb : IGeoDb
{
    private readonly ConcurrentDictionary<string, (double lon, double lat)> geo = new();
    private readonly ConcurrentDictionary<string, string> kv = new();

    public void GeoAdd(string key, double longitude, double latitude, string member)
        => geo[member] = (longitude, latitude);

    // Very rough filter: returns all currently tracked members (sufficient for sandboxing).
    public IEnumerable<string> GeoRadius(string key, double longitude, double latitude, int radiusKm)
        => geo.Keys;

    public void StringSet(string key, string value) => kv[key] = value;
    public string? StringGet(string key) => kv.TryGetValue(key, out var v) ? v : null;
    public void KeyDelete(string key) => kv.TryRemove(key, out _);
    public bool SortedSetRemove(string key, string member) => geo.TryRemove(member, out _);
}