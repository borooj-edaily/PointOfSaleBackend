namespace Pos.Api.Exceptions;

// BR-09: thrown when an authenticated, active user does not hold the specific
// permission required to perform an operation (e.g. 'process_return'). Deliberately
// distinct from NotFoundException (resource doesn't exist) and BusinessException
// (a business rule about the data was violated) -- this means "you're not allowed
// to do this", and maps to 403 Forbidden.
public class ForbiddenException : Exception
{
    public ForbiddenException(string message) : base(message)
    {
    }
}