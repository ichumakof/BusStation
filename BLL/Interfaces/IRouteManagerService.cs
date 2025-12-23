using System.Threading.Tasks;

namespace BLL.Interfaces
{
    public interface IRouteManagerService
    {
        Task<bool> CityExistsAsync(string cityName);
        Task<int> CreateCityAsync(string cityName, bool region);
        Task<int> CreateRouteAsync(int arrivalPointId, int distanceKm, int durationMinutes);
        Task<(bool Success, int RouteId, string ErrorMessage)> CreateRouteWithCityAsync(string cityName, int distanceKm, int durationMinutes);
    }
}
