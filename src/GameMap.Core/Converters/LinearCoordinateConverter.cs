namespace GameMap.Core.Converters;
/// <summary>
/// Simple linear converter: one tile equals one kilometer; origin maps to (0,0) degrees.
/// Longitude increases with X, latitude increases with Y.
/// </summary>
public sealed class LinearCoordinateConverter : ICoordinateConverter
{
    private const double KmPerDegree = 111.0; // approx

    /// <summary>
    /// Convert tile coordinates to approximate lon/lat degrees.
    /// </summary>
    public (double lon, double lat) ToRedisCoordinates(int x, int y)
    {
        double lon = x / KmPerDegree;
        double lat = y / KmPerDegree;
        return (lon, lat);
    }
}
