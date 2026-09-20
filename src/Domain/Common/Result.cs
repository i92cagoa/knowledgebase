namespace KnowledgeBase.Domain.Common;

public readonly record struct Result(bool IsSuccess, Error Error)
{
    public bool IsFailure => !IsSuccess;

    public static Result Success() => new(true, Error.None);
    public static Result Failure(Error error) => new(false, error);

    public static implicit operator Result(Error error) => Failure(error);
}

public readonly record struct Result<TValue>(bool IsSuccess, TValue? Value, Error Error)
{
    public bool IsFailure => !IsSuccess;

    public static Result<TValue> Success(TValue value) => new(true, value, Error.None);
    public static Result<TValue> Failure(Error error) => new(false, default, error);

    public static implicit operator Result<TValue>(TValue value) => Success(value);
    public static implicit operator Result<TValue>(Error error) => Failure(error);
}