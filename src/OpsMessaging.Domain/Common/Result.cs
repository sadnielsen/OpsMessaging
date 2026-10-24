namespace OpsMessaging.Domain.Common;

/// <summary>
/// The outcome of an operation that can fail in an expected way: either success or an <see cref="Common.Error"/>.
/// </summary>
/// <remarks>
/// CONCEPT (DDD): Breaking a business rule is a normal outcome, not an exceptional one. Returning a
/// Result makes that visible in the method signature, so callers cannot "forget" to handle it the way
/// they can forget a try/catch. Exceptions remain for truly unexpected situations (programming errors).
/// </remarks>
public class Result
{
    /// <summary>Creates a result; use the static factory methods instead.</summary>
    protected Result(Error? error) => Error = error;

    /// <summary>The error when the operation failed; <c>null</c> on success.</summary>
    public Error? Error { get; }

    /// <summary>True when the operation succeeded.</summary>
    public bool IsSuccess => Error is null;

    /// <summary>True when the operation failed; <see cref="Error"/> is then set.</summary>
    public bool IsFailure => !IsSuccess;

    /// <summary>A successful result without a value.</summary>
    public static Result Success() => new(null);

    /// <summary>A failed result without a value.</summary>
    public static Result Failure(Error error) => new(error);

    /// <summary>A successful result carrying <paramref name="value"/>.</summary>
    public static Result<T> Success<T>(T value) => new(value, null);

    /// <summary>A failed result for an operation that would have returned a <typeparamref name="T"/>.</summary>
    public static Result<T> Failure<T>(Error error) => new(default, error);
}

/// <summary>
/// The outcome of an operation that returns a <typeparamref name="T"/> on success.
/// </summary>
/// <typeparam name="T">The type of the value on success.</typeparam>
public sealed class Result<T> : Result
{
    private readonly T? _value;

    internal Result(T? value, Error? error)
        : base(error) => _value = value;

    /// <summary>
    /// The value on success. Reading it from a failed result is a programming error and throws.
    /// </summary>
    public T Value => IsSuccess
        ? _value!
        : throw new InvalidOperationException($"Cannot read the value of a failed result ({Error!.Code}).");
}
