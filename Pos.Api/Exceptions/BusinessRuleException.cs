namespace Pos.Api.Exceptions;

public class BusinessRuleException : BusinessException
{
    public BusinessRuleException(string message) : base(message)
    {
    }
}
