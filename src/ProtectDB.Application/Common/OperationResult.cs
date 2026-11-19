namespace ProtectDB.Application.Common;

// Результат операции без данных.
public class OperationResult
{
    public bool Success { get; }
    public string? Error { get; }

    protected OperationResult(bool success, string? error)
    {
        Success = success;
        Error = error;
    }

    public static OperationResult Ok() => new(true, null);
    public static OperationResult Fail(string error) => new(false, error);
}

// Результат операции со значением.
public class OperationResult<T> : OperationResult
{
    public T? Value { get; }

    private OperationResult(bool success, T? value, string? error) : base(success, error)
    {
        Value = value;
    }

    public static OperationResult<T> Ok(T value) => new(true, value, null);
    public static new OperationResult<T> Fail(string error) => new(false, default, error);
}
