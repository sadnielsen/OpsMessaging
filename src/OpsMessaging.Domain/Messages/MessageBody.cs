using OpsMessaging.Domain.Common;

namespace OpsMessaging.Domain.Messages;

/// <summary>The content of a message, for example the instruction employees must follow.</summary>
/// <remarks>Value object, built the same way as <see cref="MessageTitle"/>.</remarks>
public sealed record MessageBody
{
    /// <summary>Maximum number of characters (BR-01).</summary>
    public const int MaxLength = 1000;

    private MessageBody(string value) => Value = value;

    /// <summary>The body text, trimmed.</summary>
    public string Value { get; }

    /// <summary>Validates and creates a body.</summary>
    public static Result<MessageBody> Create(string? value)
    {
        var trimmed = value?.Trim();

        // BR-01: body is required, max. 1000 characters.
        if (string.IsNullOrEmpty(trimmed))
        {
            return Result.Failure<MessageBody>(MessageErrors.BodyRequired);
        }

        if (trimmed.Length > MaxLength)
        {
            return Result.Failure<MessageBody>(MessageErrors.BodyTooLong);
        }

        return Result.Success(new MessageBody(trimmed));
    }

    // CONCEPT (Privacy-by-design): see MessageTitle.ToString — content never appears in logs.
    /// <summary>Returns a placeholder instead of the content, so it cannot leak into logs.</summary>
    public override string ToString() => "[message body]";
}
