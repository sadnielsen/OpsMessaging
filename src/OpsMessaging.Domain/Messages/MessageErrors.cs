using OpsMessaging.Domain.Common;

namespace OpsMessaging.Domain.Messages;

/// <summary>
/// All expected errors of the <see cref="Message"/> aggregate, grouped per business rule.
/// </summary>
/// <remarks>
/// The error code IS the business rule code from DESIGN.md §1.2, so a failing request, a unit test and
/// a Gherkin scenario all point to the same rule.
/// </remarks>
public static class MessageErrors
{
    // BR-01: title is required, max. 100 characters; body is required, max. 1000 characters.

    /// <summary>BR-01: the title is empty.</summary>
    public static readonly Error TitleRequired =
        new("BR-01", "Title is required.", "title");

    /// <summary>BR-01: the title is longer than <see cref="MessageTitle.MaxLength"/>.</summary>
    public static readonly Error TitleTooLong =
        new("BR-01", $"Title must be at most {MessageTitle.MaxLength} characters.", "title");

    /// <summary>BR-01: the body is empty.</summary>
    public static readonly Error BodyRequired =
        new("BR-01", "Body is required.", "body");

    /// <summary>BR-01: the body is longer than <see cref="MessageBody.MaxLength"/>.</summary>
    public static readonly Error BodyTooLong =
        new("BR-01", $"Body must be at most {MessageBody.MaxLength} characters.", "body");

    /// <summary>BR-02: the audience has no role group.</summary>
    public static readonly Error AudienceWithoutRoleGroup =
        new("BR-02", "The audience must contain at least one role group.", "audience.roleGroups");

    /// <summary>BR-03: no channel at all was chosen.</summary>
    public static readonly Error NoChannels =
        new("BR-03", "A message must be sent via at least one channel.", "channels");

    /// <summary>BR-03: an urgent message with fewer than <see cref="Message.MinimumChannelsForUrgent"/> channels.</summary>
    public static readonly Error UrgentNeedsMoreChannels =
        new("BR-03", $"An urgent message must be sent via at least {Message.MinimumChannelsForUrgent} different channels.", "channels");

    /// <summary>BR-04: ValidUntil is now or in the past.</summary>
    public static readonly Error ValidUntilNotInFuture =
        new("BR-04", "ValidUntil must be in the future.", "validUntil");

    /// <summary>BR-04: ValidUntil is more than <see cref="Message.MaximumValidity"/> from now.</summary>
    public static readonly Error ValidUntilTooFarAhead =
        new("BR-04", "ValidUntil must be at most 24 hours from now.", "validUntil");

    /// <summary>BR-05: the audience resolved to zero recipients.</summary>
    public static readonly Error NoRecipients =
        new("BR-05", "The audience does not match any recipient; the message was not sent.");

    /// <summary>BR-06: only a dispatched message can be cancelled.</summary>
    public static readonly Error OnlyDispatchedCanBeCancelled =
        new("BR-06", "Only a dispatched message can be cancelled.");

    /// <summary>BR-07: an expired message can no longer be cancelled.</summary>
    public static readonly Error ExpiredCannotBeCancelled =
        new("BR-07", "An expired message can no longer be cancelled.");
}
