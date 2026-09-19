namespace Coffeeg.Api.Results;

/// <summary>
/// Outcome of a service call with no return value. Expected failure is data;
/// unexpected failure stays an exception (Docs/ARCHITECTURE.md).
/// </summary>
public class Result
{
    protected Result(bool isSuccess, Error? error)
    {
        if (isSuccess == (error is not null))
        {
            throw new ArgumentException("A success carries no error and a failure must carry one.", nameof(error));
        }

        IsSuccess = isSuccess;
        _error = error;
    }

    private readonly Error? _error;

    public bool IsSuccess { get; }

    public bool IsFailure => !IsSuccess;

    /// <summary>The failure. Throws when the result is a success.</summary>
    public Error Error => _error
        ?? throw new InvalidOperationException("A successful result has no error.");

    public static Result Success() => new(true, null);

    public static Result Failure(Error error) => new(false, error);

    public static Result<TValue> Success<TValue>(TValue value) => new(value, true, null);

    public static Result<TValue> Failure<TValue>(Error error) => new(default, false, error);
}

/// <summary>Outcome of a service call that returns a value on success.</summary>
public class Result<TValue> : Result
{
    private readonly TValue? _value;

    protected internal Result(TValue? value, bool isSuccess, Error? error)
        : base(isSuccess, error)
    {
        _value = value;
    }

    /// <summary>The value. Throws when the result is a failure.</summary>
    public TValue Value => IsSuccess
        ? _value!
        : throw new InvalidOperationException("A failed result has no value.");

    public static implicit operator Result<TValue>(TValue value) => Success(value);

    public static implicit operator Result<TValue>(Error error) => Failure<TValue>(error);
}
