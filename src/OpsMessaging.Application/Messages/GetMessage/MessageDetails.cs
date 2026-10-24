using OpsMessaging.Domain.Messages;

namespace OpsMessaging.Application.Messages.GetMessage;

/// <summary>Read model of one message, as returned by <see cref="GetMessageHandler"/>.</summary>
/// <remarks>
/// A flat snapshot instead of the <see cref="Message"/> aggregate itself: callers cannot change the
/// aggregate through it, and <see cref="Status"/> already includes the derived "Expired".
/// </remarks>
/// <param name="Id">Message ID.</param>
/// <param name="Title">Title text.</param>
/// <param name="Body">Body text.</param>
/// <param name="Priority">Normal or urgent.</param>
/// <param name="Channels">Channels used.</param>
/// <param name="RoleGroups">Role groups of the audience.</param>
/// <param name="RegionCode">Region of the audience, if any.</param>
/// <param name="ValidUntil">Until when the message is relevant.</param>
/// <param name="Status">Effective status, including Expired.</param>
/// <param name="CreatedAt">When the message was created.</param>
/// <param name="CancelledAt">When it was cancelled, if it was.</param>
/// <param name="RecipientCount">Number of recipients.</param>
/// <param name="DeliveryCount">Number of deliveries.</param>
public sealed record MessageDetails(
    Guid Id,
    string Title,
    string Body,
    Priority Priority,
    IReadOnlyList<Channel> Channels,
    IReadOnlyList<string> RoleGroups,
    string? RegionCode,
    DateTimeOffset ValidUntil,
    MessageStatus Status,
    DateTimeOffset CreatedAt,
    DateTimeOffset? CancelledAt,
    int RecipientCount,
    int DeliveryCount);
