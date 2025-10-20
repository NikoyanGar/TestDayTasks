using GameMap.Core.Converters;

namespace GameMap.Server.Services;

internal sealed class CoordinateConverter : ICoordinateConverter
{
    private const double Scale = 0.001;

    public (double longitude, double latatitude) ToRedisCoordinates(int x, int y)
    {
        double longitude = x * Scale;
        double latatitude = y * Scale;
        return (longitude, latatitude);
    }

    public (int x, int y) FromRedisCoordinates(double longitude, double latatitude)
    {
        int x = (int)(longitude / Scale);
        int y = (int)(latatitude / Scale);
        return (x, y);
    }
}
