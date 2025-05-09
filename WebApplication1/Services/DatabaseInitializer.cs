using System.Data;

namespace WebApplication1.Services;

public static class DatabaseInitializer
{
    public static void InitializeDatabase(IDbConnection db, string scriptPath)
    {
        var sql = File.ReadAllText(scriptPath);
        using var cmd = db.CreateCommand();
        cmd.CommandText = sql;
        db.Open();
        cmd.ExecuteNonQuery();
        db.Close();
    }
}