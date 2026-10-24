using OpsMessaging.Domain.Common;

namespace OpsMessaging.Domain.Messages;

/// <summary>Who sent a message: the object ID (<c>oid</c> claim) of the user or application from the access token.</summary>
/// <remarks>
/// CONCEPT (Privacy-by-design): A stable, opaque ID is enough for auditing ("who sent this?").
/// No name or e-mail address is stored.
/// </remarks>
public sealed record SenderId
{
    private SenderId(Guid value) => Value = value;

    /// <summary>The object ID.</summary>
    public Guid Value { get; }

    /// <summary>Validates and creates a sender ID.</summary>
    public static Result<SenderId> Create(Guid value)
    {
        if (value == Guid.Empty)
        {
            return Result.Failure<SenderId>(Error.Validation("senderId", "A sender ID is required."));
        }

        return Result.Success(new SenderId(value));
    }

    /// <summary>Returns the object ID as text.</summary>
    public override string ToString() => Value.ToString();
}
