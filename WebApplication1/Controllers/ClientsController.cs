using System.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using WebApplication1.Models;

namespace WebApplication1.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ClientsController : ControllerBase
{
    private readonly IDbConnection _db;
    public ClientsController(IDbConnection db) => _db = db;

    [HttpPost]
    public IActionResult Create([FromBody] ClientDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.FirstName) || string.IsNullOrWhiteSpace(dto.LastName)
                                                     || string.IsNullOrWhiteSpace(dto.Email) || string.IsNullOrWhiteSpace(dto.Pesel))
            return BadRequest("Missing required fields");

        // Simple format checks omitted for brevity

        using var cmd = _db.CreateCommand();
        cmd.CommandText = @"INSERT INTO Client(FirstName,LastName,Email,Telephone,Pesel)
                             VALUES(@fn,@ln,@em,@tel,@pesel);
                             SELECT CAST(SCOPE_IDENTITY() AS INT);";
        cmd.Parameters.Add(new SqlParameter("@fn", dto.FirstName));
        cmd.Parameters.Add(new SqlParameter("@ln", dto.LastName));
        cmd.Parameters.Add(new SqlParameter("@em", dto.Email));
        cmd.Parameters.Add(new SqlParameter("@tel", dto.Telephone ?? string.Empty));
        cmd.Parameters.Add(new SqlParameter("@pesel", dto.Pesel));

        _db.Open();
        var newId = (int)cmd.ExecuteScalar();
        _db.Close();

        return CreatedAtAction(null, new { id = newId }, new { Id = newId });
    }
}