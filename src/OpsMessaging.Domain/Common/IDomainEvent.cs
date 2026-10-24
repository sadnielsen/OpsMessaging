namespace OpsMessaging.Domain.Common;

/// <summary>
/// Marker for something meaningful that happened in the domain.
/// </summary>
/// <remarks>
/// CONCEPT (Event-driven): The aggregate only records THAT something happened. Publishing the event to
/// other systems is the job of the Application layer (via the IEventPublisher port), so the domain stays
/// free of message brokers.
/// </remarks>
public interface IDomainEvent
{
    /// <summary>When the event happened (taken from the clock passed into the domain).</summary>
    DateTimeOffset OccurredAt { get; }
}
