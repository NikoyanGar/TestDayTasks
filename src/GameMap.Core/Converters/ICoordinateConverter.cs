namespace GameMap.Core.Converters;

/// <summary>
/// Provides conversion from map Cartesian tile coordinates to Redis GEO (longitude, latitude).
/// One tile is treated as 1 kilometer; 1 km is approximately 1/111 degree.
/// </summary>
public interface ICoordinateConverter
{
    /// <summary>
    /// Converts map coordinates (in tiles) to Redis GEO coordinates (lon, lat) in degrees.
    /// </summary>
    (double lon, double lat) ToRedisCoordinates(int x, int y);
}
