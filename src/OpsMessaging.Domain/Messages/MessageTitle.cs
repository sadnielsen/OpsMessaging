using OpsMessaging.Domain.Common;

namespace OpsMessaging.Domain.Messages;

/// <summary>The short headline of a message, for example "Switch failure sector 4".</summary>
/// <remarks>
/// CONCEPT (DDD): A value object. It has no identity, only a value, and it can only exist in a valid
/// state: the constructor is private and <see cref="Create"/> enforces the rules. Once you hold a
/// MessageTitle, you never have to check its length again. The property has no setter or init accessor,
/// so a <c>with</c> expression cannot bypass the validation either.
/// </remarks>
public sealed record MessageTitle
{
    /// <summary>Maximum number of characters (BR-01).</summary>
    public const int MaxLength = 100;

    private MessageTitle(string value) => Value = value;

    /// <summary>The title text, trimmed.</summary>
    public string Value { get; }

    /// <summary>Validates and creates a title.</summary>
    public static Result<MessageTitle> Create(string? value)
    {
        var trimmed = value?.Trim();

        // BR-01: title is required, max. 100 characters.
        if (string.IsNullOrEmpty(trimmed))
        {
            return Result.Failure<MessageTitle>(MessageErrors.TitleRequired);
        }

        if (trimmed.Length > MaxLength)
        {
            return Result.Failure<MessageTitle>(MessageErrors.TitleTooLong);
        }

        return Result.Success(new MessageTitle(trimmed));
    }

    // CONCEPT (Privacy-by-design): Records print all their values in ToString(). Message content must
    // never end up in logs, so an accidental "{title}" in a log line shows a placeholder instead.
    /// <summary>Returns a placeholder instead of the content, so it cannot leak into logs.</summary>
    public override string ToString() => "[message title]";
}
