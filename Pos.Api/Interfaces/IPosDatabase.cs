using System.Data;

namespace Pos.Api.Interfaces;

public interface IPosDatabase
{
    IDbConnection Open();
}