using System.Data;
using MySqlConnector;
using Pos.Api.Interfaces;

namespace Pos.Api.Database;

public class PosDatabase : IPosDatabase
{
    private readonly string _connectionString;

    public PosDatabase(IConfiguration configuration)
    {
        _connectionString = configuration.GetConnectionString("Default")
            ?? throw new InvalidOperationException("Connection string 'Default' is not configured.");
    }

    public MySqlConnection Open()
    {
        var connection = new MySqlConnection(_connectionString);
        connection.Open();
        return connection;
    }

    IDbConnection IPosDatabase.Open() => Open();
}