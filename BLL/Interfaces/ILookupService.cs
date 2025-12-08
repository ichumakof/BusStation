using BLL.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BLL.Services
{
    public interface ILookupService
    {
        Task<IList<CityDTO>> GetCitiesAsync();
        Task<IList<TripDTO>> GetTripsByArrivalCityAndDateAsync(int arrivalCityId, DateTime date);
    }
}
