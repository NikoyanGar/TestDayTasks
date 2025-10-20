namespace GameMap.Core.Storage;

public interface IGeoDb
{
    // GEO operations
    void GeoAdd(string key, double longitude, double latitude, string member);
    IEnumerable<string> GeoRadius(string key, double longitude, double latitude, int radiusKm);

    // String ops
    void StringSet(string key, string value);
    string? StringGet(string key);

    // Key ops
    void KeyDelete(string key);

    // Sorted set ops
    bool SortedSetRemove(string key, string member);
}
