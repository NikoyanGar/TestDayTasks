
namespace GameMap.Core.Features.Regions
{
    public interface IRegionLayer
    {
        int Height { get; }
        int Width { get; }

        Region GetRegionById(ushort id);
        ushort GetRegionId(int x, int y);
        IEnumerable<Region> GetRegionsInArea(int x, int y, int w, int h);
        bool IsTileInRegion(int x, int y, ushort regionId);
    }
}