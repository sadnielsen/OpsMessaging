using OpsMessaging.Application.Abstractions;
using OpsMessaging.Domain.Messages;

namespace OpsMessaging.Application.UnitTests.TestDoubles;

/// <summary>In-memory repository that also records which messages were added and updated.</summary>
internal sealed class FakeMessageRepository : IMessageRepository
{
    private readonly Dictionary<Guid, Message> _messages = [];

    public List<Message> Added { get; } = [];

    public List<Message> Updated { get; } = [];

    public Task AddAsync(Message message, CancellationToken cancellationToken)
    {
        _messages[message.Id] = message;
        Added.Add(message);
        return Task.CompletedTask;
    }

    public Task<Message?> GetByIdAsync(Guid id, CancellationToken cancellationToken) =>
        Task.FromResult(_messages.GetValueOrDefault(id));

    public Task UpdateAsync(Message message, CancellationToken cancellationToken)
    {
        _messages[message.Id] = message;
        Updated.Add(message);
        return Task.CompletedTask;
    }
}
