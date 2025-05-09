using System.Data;
using Microsoft.AspNetCore.Mvc;
using WebApplication1.Models;

namespace WebApplication1.Controllers;

[ApiController]
[Route("api/[controller]")]
public class TripsController : ControllerBase
{
    private readonly IDbConnection _db;
    public TripsController(IDbConnection db) => _db = db;

    [HttpGet]
    public IActionResult GetAll()
    {
        using var cmd = _db.CreateCommand();
        cmd.CommandText = @"SELECT t.IdTrip, t.Name, t.Description, t.DateFrom, t.DateTo, t.MaxPeople,
                                  c.Name AS Country
                           FROM Trip t
                           LEFT JOIN Country_Trip ct ON t.IdTrip = ct.IdTrip
                           LEFT JOIN Country c ON ct.IdCountry = c.IdCountry";

        var trips = new Dictionary<int, TripDto>();
        _db.Open();
        using var reader = cmd.ExecuteReader();
        while (reader.Read())
        {
            var id = reader.GetInt32(0);
            if (!trips.ContainsKey(id))
            {
                trips[id] = new TripDto
                {
                    IdTrip = id,
                    Name = reader.GetString(1),
                    Description = reader.GetString(2),
                    DateFrom = reader.GetDateTime(3),
                    DateTo = reader.GetDateTime(4),
                    MaxPeople = reader.GetInt32(5)
                };
            }
        }
        _db.Close();

        return Ok(trips.Values);
    }
}