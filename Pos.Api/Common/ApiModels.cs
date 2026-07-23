namespace Pos.Api.Common;

public class ApiErrorResponse
{
    public bool Success { get; set; } = false;
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    public string Message { get; set; } = string.Empty;
    public List<string> Errors { get; set; } = new();
}
