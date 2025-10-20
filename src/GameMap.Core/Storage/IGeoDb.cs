namespace GameMap.Core.Storage;

/// <summary>
/// Minimal abstraction for GEO-capable storage used by the object layer.
/// Distances and radius are assumed to be in kilometers.
/// </summary>
public interface IGeoDb
{
    void GeoAdd(string key, double lon, double lat, string member);
    IEnumerable<string> GeoRadius(string key, double lon, double lat, double radiusKm);

    void StringSet(string key, string value);
    string? StringGet(string key);

    void KeyDelete(string key);
    void SortedSetRemove(string key, string member);
}
