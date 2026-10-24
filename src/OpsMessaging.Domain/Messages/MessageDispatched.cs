using OpsMessaging.Domain.Common;

namespace OpsMessaging.Domain.Messages;

/// <summary>
/// Domain event: a message was dispatched to its recipients.
/// </summary>
/// <remarks>
/// CONCEPT (Event-driven): Other bounded contexts (for example reporting) can react to this event
/// without OpsMessaging knowing them. The event carries IDs and counts only.
/// CONCEPT (Privacy-by-design): No title, body or recipient list: a consumer that needs details must
/// ask for them with its own authorisation.
/// </remarks>
/// <param name="MessageId">The dispatched message.</param>
/// <param name="SenderId">Who sent it (object ID).</param>
/// <param name="Priority">The priority of the message.</param>
/// <param name="Channels">The channels used.</param>
/// <param name="RecipientCount">Number of distinct recipients.</param>
/// <param name="DeliveryCount">Number of deliveries (recipients × channels).</param>
/// <param name="OccurredAt">When the message was dispatched.</param>
public sealed record MessageDispatched(
    Guid MessageId,
    SenderId SenderId,
    Priority Priority,
    IReadOnlyList<Channel> Channels,
    int RecipientCount,
    int DeliveryCount,
    DateTimeOffset OccurredAt) : IDomainEvent;
