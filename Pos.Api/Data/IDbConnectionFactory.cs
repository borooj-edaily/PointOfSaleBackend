using System.Data;
using MySqlConnector;

namespace Pos.Api.Data;

public interface IDbConnectionFactory
{
    IDbConnection CreateConnection();
}

public class MySqlConnectionFactory : IDbConnectionFactory
{
    private readonly string _connectionString;

    public MySqlConnectionFactory(IConfiguration configuration)
    {
        // Read from environment variable first (set via .env / DotNetEnv),
        // fall back to appsettings for local dev convenience.
        _connectionString = Environment.GetEnvironmentVariable("MYSQL_CONNECTION_STRING")
            ?? configuration.GetConnectionString("Default")
            ?? throw new InvalidOperationException("MySQL connection string is not configured.");
    }

    public IDbConnection CreateConnection() => new MySqlConnection(_connectionString);
}
