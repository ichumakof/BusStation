using BLL.Models;
using DAL;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace BLL.Interfaces
{
    public interface IScheduleService
    {
        Task<IList<RouteDTO>> GetRoutesAsync();
        Task<IList<SimpleItemDTO>> GetDriversAsync();
        Task<IList<SimpleItemDTO>> GetBusesAsync();
        Task<int> GenerateScheduleAsync(ScheduleGenerationRequest request);
    }
}