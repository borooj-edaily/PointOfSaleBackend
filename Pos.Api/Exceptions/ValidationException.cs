namespace Pos.Api.Exceptions;

public class ValidationException : Exception
{
    public List<string> Errors { get; }

    public ValidationException(string message) : base(message)
    {
        Errors = new List<string> { message };
    }

    public ValidationException(IEnumerable<string> errors)
        : base(string.Join(" | ", errors))
    {
        Errors = errors.ToList();
    }
}
