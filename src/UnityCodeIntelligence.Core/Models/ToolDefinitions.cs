namespace UnityCodeIntelligence.Core.Models;

public class ToolResult
{
    public bool IsSuccess { get; }
    public object? Value { get; }
    public string? Error { get; }

    private ToolResult(bool isSuccess, object? value, string? error = null)
    {
        IsSuccess = isSuccess;
        Value = value;
        Error = error;
    }

    public static ToolResult Success(object? value) => new(true, value);
    public static ToolResult Failure(string error) => new(false, null, error);
}
