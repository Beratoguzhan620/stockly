namespace Stockly.Application.Common;

public enum ResultStatus
{
    Ok,
    Invalid,
    NotFound,
}

/// <summary>Outcome of a service call: success, a validation message for the user, or not found.</summary>
public class Result
{
    public ResultStatus Status { get; init; }
    public string Message { get; init; } = string.Empty;
    public bool Success => Status == ResultStatus.Ok;

    public static Result Ok(string message = "") => new() { Status = ResultStatus.Ok, Message = message };
    public static Result Invalid(string message) => new() { Status = ResultStatus.Invalid, Message = message };
    public static Result NotFound(string message = "Kayıt bulunamadı.") => new() { Status = ResultStatus.NotFound, Message = message };
}

public sealed class Result<T> : Result
{
    public T? Value { get; init; }

    public static Result<T> Ok(T value, string message = "") => new() { Status = ResultStatus.Ok, Value = value, Message = message };
    public static new Result<T> Invalid(string message) => new() { Status = ResultStatus.Invalid, Message = message };
    public static new Result<T> NotFound(string message = "Kayıt bulunamadı.") => new() { Status = ResultStatus.NotFound, Message = message };
}

public sealed class PagedList<T>
{
    public List<T> Items { get; init; } = [];
    public int Page { get; init; }
    public int PageSize { get; init; }
    public int TotalCount { get; init; }
    public int TotalPages => PageSize <= 0 ? 0 : Math.Max(1, (int)Math.Ceiling(TotalCount / (double)PageSize));
}
