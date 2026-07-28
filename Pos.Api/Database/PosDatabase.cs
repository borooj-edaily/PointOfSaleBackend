using System.Data;
using MySqlConnector;
using Pos.Api.Interfaces;
using System.Diagnostics;

namespace Pos.Api.Database;

public class PosDatabase : IPosDatabase
{
    private readonly string _connectionString;

    public PosDatabase(IConfiguration configuration)
    {
        _connectionString = configuration.GetConnectionString("Default")
            ?? throw new InvalidOperationException("Connection string 'Default' is not configured.");
            System.Diagnostics.Debug.WriteLine($"PosDatabase initialized with connection string: {_connectionString}");
    }

    public MySqlConnection Open()
    {
        var connection = new MySqlConnection(_connectionString);
        System.Diagnostics.Debug.WriteLine($"Opening database connection to: {_connectionString}");
        connection.Open();
        return connection;
    }

    IDbConnection IPosDatabase.Open() => Open();
}