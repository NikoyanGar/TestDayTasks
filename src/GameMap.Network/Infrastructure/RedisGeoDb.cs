using GameMap.Core.Storage;
using StackExchange.Redis;
using System.Collections.Generic;

namespace GameMap.Network.Infrastructure;

/// <summary>
/// IGeoDb implementation backed by StackExchange.Redis.
/// </summary>
public sealed class RedisGeoDb : IGeoDb
{
    private readonly IDatabase _db;

    public RedisGeoDb(IDatabase db) => _db = db;

    public void GeoAdd(string key, double lon, double lat, string member)
        => _db.GeoAdd(key, lon, lat, member);

    public IEnumerable<string> GeoRadius(string key, double lon, double lat, double radiusKm)
    {
        var res = _db.GeoRadius(key, lon, lat, radiusKm, GeoUnit.Kilometers);
        foreach (var e in res)
            yield return e.Member!;
    }

    public void StringSet(string key, string value) => _db.StringSet(key, value);
    public string? StringGet(string key) => _db.StringGet(key);
    public void KeyDelete(string key) => _db.KeyDelete(key);
    public void SortedSetRemove(string key, string member) => _db.SortedSetRemove(key, member);
}
