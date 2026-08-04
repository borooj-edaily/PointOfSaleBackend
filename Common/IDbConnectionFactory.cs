using System.Data;

namespace Pos.Api.Common
{
    public interface IDbConnectionFactory
    {
        IDbConnection CreateConnection();
    }
}