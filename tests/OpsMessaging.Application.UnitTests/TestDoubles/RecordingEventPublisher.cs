using OpsMessaging.Application.Abstractions;
using OpsMessaging.Domain.Common;

namespace OpsMessaging.Application.UnitTests.TestDoubles;

/// <summary>Collects published events so a test can check what was (not) published.</summary>
internal sealed class RecordingEventPublisher : IEventPublisher
{
    public List<IDomainEvent> Published { get; } = [];

    public Task PublishAsync(IDomainEvent domainEvent, CancellationToken cancellationToken)
    {
        Published.Add(domainEvent);
        return Task.CompletedTask;
    }
}
