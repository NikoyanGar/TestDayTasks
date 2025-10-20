using GameMap.Core;

namespace GameMap.Network.Udp;

public class MapQueryProvider : IMapQueryProvider
{
    private readonly MapManager mapManager;
    private readonly RegionLayer regionLayer;

    public MapQueryProvider(MapManager mm, RegionLayer rl)
    {
        mapManager = mm;
        regionLayer = rl;
    }

    public IEnumerable<MapObject> GetObjectsInArea(int x1, int y1, int x2, int y2)
    {
        return mapManager.GetObjectsInArea(x1, y1, x2, y2);
    }

    public IEnumerable<Region> GetRegionsInArea(int x1, int y1, int x2, int y2)
    {
        return regionLayer.GetRegionsInArea(x1, y1, x2 - x1 + 1, y2 - y1 + 1);
    }
}
