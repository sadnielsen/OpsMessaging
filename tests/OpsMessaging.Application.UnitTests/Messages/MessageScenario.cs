using OpsMessaging.Application.Messages.SendMessage;
using OpsMessaging.Application.UnitTests.TestDoubles;
using OpsMessaging.Domain.Messages;

namespace OpsMessaging.Application.UnitTests.Messages;

/// <summary>
/// Shared setup for the read and cancel use cases: fakes plus a message that was sent through the
/// real <see cref="SendMessageHandler"/>, so tests start from a realistic state.
/// </summary>
internal sealed class MessageScenario
{
    public FakeMessageRepository Repository { get; } = new();

    public FakeClock Clock { get; } = new();

    /// <summary>Sends an urgent message (Push + Sms) to EMP-0001 and EMP-0002; returns its ID.</summary>
    public async Task<Guid> SendMessageAsync(TimeSpan validFor)
    {
        var handler = new SendMessageHandler(
            Repository,
            new FakeRecipientDirectory().WithRecipients("EMP-0001", "EMP-0002"),
            new RecordingChannelDispatcher(),
            new RecordingEventPublisher(),
            Clock,
            new FakeCurrentUser());

        var command = new SendMessageCommand(
            "Shift change", "From 14:00 the timetable changes.", Priority.Urgent,
            [Channel.Push, Channel.Sms], ["Driver"], null, Clock.UtcNow.Add(validFor));

        var result = await handler.HandleAsync(command, TestContext.Current.CancellationToken);
        return result.Value.Id;
    }
}
