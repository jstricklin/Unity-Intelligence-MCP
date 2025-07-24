namespace UnityIntelligenceMCP.Models;
public class ResourceResult
{
    public bool IsSuccess { get; }
    public string Status { get; }
    // [QUESTION] can this object data below be a string type? the hope is that it would be serialized into JSON on final return
    public object? Data { get; }
    public int? ErrorCode { get; }
    public Type? DataType { get; }

    private ResourceResult(bool isSuccess, string status, object? data, int? errorCode, Type? dataType)
    {
        IsSuccess = isSuccess;
        Status = status;
        Data = data;
        ErrorCode = errorCode;
        DataType = DataType;
    }

    public static ResourceResult Success<T>(T data, string status = "Success") =>
        new(true, status, data, null, typeof(T));

    public static ResourceResult Error(int code, string message) =>
        new(false, message, null, code, null);

    public static ResourceResult Error(string message) =>
        Error(400, message);

    // Safe casting helper
    public T? GetData<T>() => Data is T result ? result : default;
}