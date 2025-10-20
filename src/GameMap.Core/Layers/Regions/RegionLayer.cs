using System.Collections.Concurrent;

namespace GameMap.Core.Features.Regions;

/// <summary>
/// Equal-area rectangular tiling region layer.
/// Partitions the map into RxC rectangles of equal area; each tile belongs to exactly one region.
/// </summary>
public sealed class RegionLayer : IRegionLayer
{
    private readonly ushort[] tiles;
    private readonly Dictionary<ushort, Region> regions;

    private readonly int regionsPerRow;
    private readonly int regionsPerCol;
    private readonly int regionWidth;
    private readonly int regionHeight;

    public int Width { get; }
    public int Height { get; }

    public RegionLayer(int width, int height, int regionCount)
    {
        if (width <= 0 || height <= 0) throw new ArgumentOutOfRangeException();
        if (regionCount <= 0) throw new ArgumentOutOfRangeException(nameof(regionCount));

        Width = width;
        Height = height;

        // Choose rows/cols factoring regionCount to match aspect ratio.
        (regionsPerRow, regionsPerCol) = ChooseGrid(regionCount, Width, Height);

        if (Width % regionsPerRow != 0 || Height % regionsPerCol != 0)
            throw new ArgumentException(
                $"For equal-area regions, Width must be divisible by cols ({regionsPerRow}) and Height by rows ({regionsPerCol}). " +
                $"Got Width={Width}, Height={Height}, cols={regionsPerRow}, rows={regionsPerCol}");

        regionWidth = Width / regionsPerRow;
        regionHeight = Height / regionsPerCol;
        if (regionWidth <= 0 || regionHeight <= 0) throw new ArgumentOutOfRangeException(nameof(regionCount));

        tiles = new ushort[Width * Height];
        regions = new Dictionary<ushort, Region>(regionCount);

        GenerateRegions(regionCount);
    }

    private static (int cols, int rows) ChooseGrid(int regionCount, int width, int height)
    {
        // Find factor pair cols*rows=regionCount close to map aspect ratio (cols/rows ~ width/height).
        double target = (double)width / height;
        int bestCols = 1, bestRows = regionCount;
        double bestDiff = double.MaxValue;

        for (int rows = 1; rows * rows <= regionCount; rows++)
        {
            if (regionCount % rows != 0) continue;
            int cols = regionCount / rows;

            // Try (cols, rows)
            double ratio1 = (double)cols / rows;
            double diff1 = Math.Abs(ratio1 - target);
            if (diff1 < bestDiff) { bestDiff = diff1; bestCols = cols; bestRows = rows; }

            // Try swapped (rows, cols) to cover both orientations if desired
            double ratio2 = (double)rows / cols;
            double diff2 = Math.Abs(ratio2 - target);
            if (diff2 < bestDiff) { bestDiff = diff2; bestCols = rows; bestRows = cols; }
        }

        return (bestCols, bestRows);
    }

    private void GenerateRegions(int regionCount)
    {
        ushort id = 1;
        for (int ry = 0; ry < regionsPerCol; ry++)
        {
            for (int rx = 0; rx < regionsPerRow; rx++)
            {
                if (id > regionCount) throw new InvalidOperationException("Internal generation error.");

                var region = new Region(id, $"Region_{id}");
                regions[id] = region;

                int xStart = rx * regionWidth;
                int yStart = ry * regionHeight;

                for (int y = 0; y < regionHeight; y++)
                {
                    int globalY = yStart + y;
                    int idx = globalY * Width + xStart;
                    for (int x = 0; x < regionWidth; x++)
                        tiles[idx + x] = id;
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
        if (w <= 0 || h <= 0) return Array.Empty<Region>();

        int x1 = x;
        int y1 = y;
        int x2 = x + w - 1;
        int y2 = y + h - 1;

        // Clamp to map bounds; if no intersection, return empty
        if (x2 < 0 || y2 < 0 || x1 >= Width || y1 >= Height) return Array.Empty<Region>();
        x1 = Math.Max(0, x1);
        y1 = Math.Max(0, y1);
        x2 = Math.Min(Width - 1, x2);
        y2 = Math.Min(Height - 1, y2);

        // Compute region cells intersected by area (no per-tile scan).
        int rx1 = x1 / regionWidth;
        int rx2 = x2 / regionWidth;
        int ry1 = y1 / regionHeight;
        int ry2 = y2 / regionHeight;

        var set = new HashSet<ushort>();
        for (int ry = ry1; ry <= ry2; ry++)
        {
            int yBase = ry * regionHeight;
            for (int rx = rx1; rx <= rx2; rx++)
            {
                int xBase = rx * regionWidth;
                // pick any tile from the region rectangle (top-left)
                ushort id = tiles[(yBase * Width) + xBase];
                set.Add(id);
            }
        }

        return set.Select(id => regions[id]);
    }
}
