namespace GameMap.Network.Udp;

public interface IMapQueryProvider
{
    IEnumerable<MapObject> GetObjectsInArea(int x1, int y1, int x2, int y2);
    IEnumerable<Region> GetRegionsInArea(int x1, int y1, int x2, int y2);
}
