using System.Data;
using MySqlConnector;

namespace Pos.Api.Common
{
    public class MySqlConnectionFactory : IDbConnectionFactory
    {
        private readonly string _connectionString;

        public MySqlConnectionFactory(IConfiguration configuration)
        {
            _connectionString = Environment.GetEnvironmentVariable("DB_CONNECTION_STRING")
                ?? throw new InvalidOperationException("DB_CONNECTION_STRING not found in .env");
        }

        public IDbConnection CreateConnection()
        {
            return new MySqlConnection(_connectionString);
        }
    }
}