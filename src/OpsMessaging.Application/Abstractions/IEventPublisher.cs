using OpsMessaging.Domain.Common;

namespace OpsMessaging.Application.Abstractions;

/// <summary>Publishes domain events to other systems (for example via a message broker).</summary>
/// <remarks>
/// CONCEPT (Event-driven): The domain raises events; this port makes them visible outside this service.
/// Consumers are decoupled: OpsMessaging does not know who listens.
/// </remarks>
public interface IEventPublisher
{
    /// <summary>Publishes one domain event.</summary>
    Task PublishAsync(IDomainEvent domainEvent, CancellationToken cancellationToken);
}
