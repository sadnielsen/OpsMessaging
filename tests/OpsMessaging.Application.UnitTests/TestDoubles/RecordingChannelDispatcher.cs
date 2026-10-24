using OpsMessaging.Application.Abstractions;
using OpsMessaging.Domain.Messages;

namespace OpsMessaging.Application.UnitTests.TestDoubles;

/// <summary>Records every send attempt; channels in <see cref="FailingChannels"/> report a failure.</summary>
internal sealed class RecordingChannelDispatcher : IChannelDispatcher
{
    public List<(Guid MessageId, RecipientId RecipientId, Channel Channel)> Attempts { get; } = [];

    public HashSet<Channel> FailingChannels { get; } = [];

    public Task<bool> TrySendAsync(Message message, RecipientId recipientId, Channel channel, CancellationToken cancellationToken)
    {
        Attempts.Add((message.Id, recipientId, channel));
        return Task.FromResult(!FailingChannels.Contains(channel));
    }
}
