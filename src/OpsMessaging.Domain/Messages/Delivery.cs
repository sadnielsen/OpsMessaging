namespace OpsMessaging.Domain.Messages;

/// <summary>
/// The delivery of one message to one recipient via one channel, with its status.
/// </summary>
/// <remarks>
/// CONCEPT (DDD): An entity inside the Message aggregate. Its identity is local to the aggregate:
/// the combination (<see cref="RecipientId"/>, <see cref="Channel"/>) is unique within one message.
/// Outside code cannot change a delivery directly (the state-changing methods are internal); it goes
/// through the aggregate root, which guards the consistency of the whole message.
/// </remarks>
public sealed class Delivery
{
    internal Delivery(RecipientId recipientId, Channel channel)
    {
        RecipientId = recipientId;
        Channel = channel;
        Status = DeliveryStatus.Pending;
    }

    /// <summary>Pseudonymous ID of the recipient. No contact details (privacy-by-design).</summary>
    public RecipientId RecipientId { get; }

    /// <summary>The channel used for this delivery.</summary>
    public Channel Channel { get; }

    /// <summary>Current status: Pending, then Sent or Failed.</summary>
    public DeliveryStatus Status { get; private set; }

    internal void MarkSent() => MoveTo(DeliveryStatus.Sent);

    internal void MarkFailed() => MoveTo(DeliveryStatus.Failed);

    private void MoveTo(DeliveryStatus newStatus)
    {
        // Allowed transitions: Pending -> Sent and Pending -> Failed (DESIGN.md §1.3).
        // Reporting a result twice is a programming error, not a business rule, hence an exception.
        if (Status != DeliveryStatus.Pending)
        {
            throw new InvalidOperationException(
                $"Delivery to {RecipientId} via {Channel} is already {Status}; it cannot become {newStatus}.");
        }

        Status = newStatus;
    }
}
