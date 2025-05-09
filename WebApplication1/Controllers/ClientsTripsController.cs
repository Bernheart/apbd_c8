using System.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;

namespace WebApplication1.Controllers;

[ApiController]
[Route("api/clients/{clientId}/trips")]
public class ClientsTripsController : ControllerBase
{
    private readonly IDbConnection _db;
    public ClientsTripsController(IDbConnection db) => _db = db;

    [HttpGet]
    public IActionResult GetByClient(int clientId)
    {
        // Validate client exists
        using var check = _db.CreateCommand();
        check.CommandText = "SELECT COUNT(*) FROM Client WHERE IdClient = @id";
        var pid = new SqlParameter("@id", clientId);
        check.Parameters.Add(pid);
        _db.Open();
        var exists = (int)check.ExecuteScalar() > 0;
        if (!exists) { _db.Close(); return NotFound("Client not found"); }
        _db.Close();

        // Fetch trips
        using var cmd = _db.CreateCommand();
        cmd.CommandText = @"SELECT t.IdTrip, t.Name, t.Description, t.DateFrom, t.DateTo, t.MaxPeople,
                                   ct.RegisteredAt, ct.PaymentDate
                            FROM Trip t
                            INNER JOIN Client_Trip ct ON t.IdTrip = ct.IdTrip
                            WHERE ct.IdClient = @cid";
        cmd.Parameters.Add(new SqlParameter("@cid", clientId));

        var list = new List<object>();
        _db.Open();
        using var reader = cmd.ExecuteReader();
        while (reader.Read())
        {
            list.Add(new {
                IdTrip = reader.GetInt32(0),
                Name = reader.GetString(1),
                Description = reader.GetString(2),
                DateFrom = reader.GetDateTime(3),
                DateTo = reader.GetDateTime(4),
                MaxPeople = reader.GetInt32(5),
                RegisteredAt = reader.GetInt32(6),
                PaymentDate = reader.GetInt32(7)
            });
        }
        _db.Close();
        return Ok(list);
    }

    // 4. PUT /api/clients/{clientId}/trips/{tripId}
    [HttpPut("{tripId}")]
    public IActionResult Register(int clientId, int tripId)
    {
        _db.Open();
        // Check client & trip exist
        using var check = _db.CreateCommand();
        check.CommandText = @"SELECT (SELECT COUNT(*) FROM Client WHERE IdClient=@c),
                                    (SELECT COUNT(*) FROM Trip WHERE IdTrip=@t),
                                    (SELECT COUNT(*) FROM Client_Trip WHERE IdClient=@c AND IdTrip=@t),
                                    t.MaxPeople,
                                    (SELECT COUNT(*) FROM Client_Trip WHERE IdTrip=@t)
                             FROM Trip t WHERE t.IdTrip=@t";
        check.Parameters.Add(new SqlParameter("@c", clientId));
        check.Parameters.Add(new SqlParameter("@t", tripId));
        using var reader = check.ExecuteReader();
        if (!reader.Read()) { _db.Close(); return NotFound("Trip not found"); }
        var clientExists = reader.GetInt32(0)>0;
        var tripExists = reader.GetInt32(1)>0;
        var already = reader.GetInt32(2)>0;
        var maxP = reader.GetInt32(3);
        var current = reader.GetInt32(4);
        reader.Close();

        if (!clientExists) { _db.Close(); return NotFound("Client not found"); }
        if (!tripExists)   { _db.Close(); return NotFound("Trip not found"); }
        if (already)       { _db.Close(); return Conflict("Already registered"); }
        if (current >= maxP){ _db.Close(); return Conflict("Full"); }

        // Insert registration
        using var ins = _db.CreateCommand();
        ins.CommandText = @"INSERT INTO Client_Trip(IdClient,IdTrip,RegisteredAt,PaymentDate)
                             VALUES(@c,@t,@r,0)";
        ins.Parameters.Add(new SqlParameter("@c", clientId));
        ins.Parameters.Add(new SqlParameter("@t", tripId));
        int dateCode = int.Parse(DateTime.Now.ToString("yyyyMMdd"));
        ins.Parameters.Add(new SqlParameter("@r", dateCode));
        ins.ExecuteNonQuery();
        _db.Close();
        return Ok("Registered");
    }

    // 5. DELETE /api/clients/{clientId}/trips/{tripId}
    [HttpDelete("{tripId}")]
    public IActionResult Unregister(int clientId, int tripId)
    {
        using var cmd = _db.CreateCommand();
        cmd.CommandText = @"DELETE FROM Client_Trip WHERE IdClient=@c AND IdTrip=@t";
        cmd.Parameters.Add(new SqlParameter("@c", clientId));
        cmd.Parameters.Add(new SqlParameter("@t", tripId));
        _db.Open();
        var affected = cmd.ExecuteNonQuery();
        _db.Close();
        if (affected == 0) return NotFound("Registration not found");
        return Ok("Unregistered");
    }
}