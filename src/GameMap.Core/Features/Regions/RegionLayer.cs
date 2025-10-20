using System.Collections.Concurrent;

namespace GameMap.Core.Features.Regions;

/// <summary>
/// Simple equal-area rectangular tiling region layer.
/// Partitions the map into RxC rectangles of equal area; each tile belongs to exactly one region.
/// </summary>
public sealed class RegionLayer : IRegionLayer
{
    private readonly ushort[] tiles;
    private readonly Dictionary<ushort, Region> regions;
    public int Width { get; }
    public int Height { get; }

    public RegionLayer(int width, int height, int regionCount)
    {
        if (width <= 0 || height <= 0)
            throw new ArgumentOutOfRangeException();
        if (regionCount <= 0)
            throw new ArgumentOutOfRangeException(nameof(regionCount));

        Width = width;
        Height = height;
        tiles = new ushort[Width * Height];
        regions = new Dictionary<ushort, Region>(regionCount);

        GenerateRegions(regionCount);
    }

    private void GenerateRegions(int regionCount)
    {
        // равномерное деление карты на регионы (грид)
        int regionsPerRow = (int)Math.Sqrt(regionCount);
        int regionsPerCol = regionCount / regionsPerRow;

        int regionWidth = Width / regionsPerRow;
        int regionHeight = Height / regionsPerCol;

        ushort id = 1;

        for (int ry = 0; ry < regionsPerCol; ry++)
        {
            for (int rx = 0; rx < regionsPerRow; rx++)
            {
                var region = new Region(id, $"Region_{id}");
                regions[id] = region;

                // заполнение тайлов региона
                for (int y = 0; y < regionHeight; y++)
                {
                    for (int x = 0; x < regionWidth; x++)
                    {
                        int globalX = rx * regionWidth + x;
                        int globalY = ry * regionHeight + y;
                        int idx = globalY * Width + globalX;
                        tiles[idx] = id;
                    }
                }

                id++;
            }
        }
    }

    public ushort GetRegionId(int x, int y)
    {
        if (x < 0 || y < 0 || x >= Width || y >= Height)
            throw new ArgumentOutOfRangeException();
        return tiles[y * Width + x];
    }

    public Region GetRegionById(ushort id) =>
        regions.TryGetValue(id, out var region) ? region
        : throw new KeyNotFoundException($"Region {id} not found");

    public bool IsTileInRegion(int x, int y, ushort regionId) =>
        GetRegionId(x, y) == regionId;

    public IEnumerable<Region> GetRegionsInArea(int x, int y, int w, int h)
    {
        var result = new HashSet<ushort>();

        for (int yy = y; yy < y + h; yy++)
        {
            for (int xx = x; xx < x + w; xx++)
            {
                if (xx >= 0 && yy >= 0 && xx < Width && yy < Height)
                    result.Add(GetRegionId(xx, yy));
            }
        }

        return result.Select(id => regions[id]);
    }
}
