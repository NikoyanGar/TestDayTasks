using GameMap.Core.Storage;
using StackExchange.Redis;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GameMap.Server.Services
{
    //TODO: fix on debug mode
    public sealed class RedisGeoDb : IGeoDb
    {
        private readonly IDatabase _db;

        public RedisGeoDb(IDatabase db) => _db = db;

        public void GeoAdd(string key, double lon, double lat, string member)
            => _db.GeoAdd(key, lon, lat, member);

        public void StringSet(string key, string value) => _db.StringSet(key, value);
        public string? StringGet(string key) => _db.StringGet(key);
        public void KeyDelete(string key) => _db.KeyDelete(key);
        public IEnumerable<string> GeoRadius(string key, double longitude, double latitude, int radiusKm)
        {
            var res = _db.GeoRadius(key, longitude, latitude, radiusKm, GeoUnit.Kilometers);
            foreach (var e in res)
                yield return e.Member!;
        }

        bool IGeoDb.SortedSetRemove(string key, string member)
        {
            return _db.SortedSetRemove(key, member);
        }
    }
}
