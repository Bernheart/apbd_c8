using WebApplication1.Models;

namespace WebApplication1.Services;

public interface ITripsService
{
    Task<List<TripDto>> GetTrips();
}