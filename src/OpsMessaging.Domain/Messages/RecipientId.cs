using OpsMessaging.Domain.Common;

namespace OpsMessaging.Domain.Messages;

/// <summary>Pseudonymous identifier of an employee who receives a message, for example <c>EMP-0001</c>.</summary>
/// <remarks>
/// CONCEPT (Privacy-by-design): The domain only knows this pseudonymous ID: never a name, phone number or
/// push token. Contact details are looked up by the channel adapter at the moment of sending and are
/// never stored in a Message or Delivery (data minimisation).
/// </remarks>
public sealed record RecipientId
{
    /// <summary>Maximum length of an identifier.</summary>
    public const int MaxLength = 50;

    private RecipientId(string value) => Value = value;

    /// <summary>The identifier.</summary>
    public string Value { get; }

    /// <summary>Validates and creates a recipient ID.</summary>
    public static Result<RecipientId> Create(string? value)
    {
        var trimmed = value?.Trim();

        if (string.IsNullOrEmpty(trimmed) || trimmed.Length > MaxLength)
        {
            return Result.Failure<RecipientId>(
                Error.Validation("recipientId", $"A recipient ID is required and at most {MaxLength} characters."));
        }

        return Result.Success(new RecipientId(trimmed));
    }

    /// <summary>Returns the identifier itself (it is pseudonymous, so safe to show).</summary>
    public override string ToString() => Value;
}
