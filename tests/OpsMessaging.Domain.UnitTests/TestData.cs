using OpsMessaging.Domain.Common;
using OpsMessaging.Domain.Messages;

namespace OpsMessaging.Domain.UnitTests;

/// <summary>
/// Valid default values, so each test only spells out the one thing it is about.
/// </summary>
// CONCEPT (Unit test): A fixed "now" instead of the system clock makes every test deterministic:
// the same input always gives the same result, today and next year.
internal static class TestData
{
    public static readonly DateTimeOffset Now = new(2026, 10, 24, 12, 0, 0, TimeSpan.Zero);

    public static readonly SenderId Sender = SenderId.Create(Guid.Parse("11111111-1111-1111-1111-111111111111")).Value;

    public static MessageTitle Title(string value = "Switch failure sector 4") => MessageTitle.Create(value).Value;

    public static MessageBody Body(string value = "Expect delays. Follow the instructions of traffic control.") =>
        MessageBody.Create(value).Value;

    public static Audience DefaultAudience() =>
        Audience.Create(["Driver"], "REGION-NORTH").Value;

    public static RecipientId Recipient(string value) => RecipientId.Create(value).Value;

    /// <summary>Calls <see cref="Message.Create"/> with valid defaults; override what the test is about.</summary>
    public static Result<Message> CreateMessage(
        Priority priority = Priority.Normal,
        Channel[]? channels = null,
        DateTimeOffset? validUntil = null) =>
        Message.Create(
            Title(),
            Body(),
            priority,
            channels ?? [Channel.Push],
            DefaultAudience(),
            validUntil ?? Now.AddHours(2),
            Sender,
            Now);

    /// <summary>A message that is already dispatched to two recipients via Push and Sms.</summary>
    public static Message DispatchedMessage(DateTimeOffset? validUntil = null)
    {
        var message = CreateMessage(Priority.Urgent, [Channel.Push, Channel.Sms], validUntil).Value;
        message.Dispatch([Recipient("EMP-0001"), Recipient("EMP-0002")], Now).IsSuccess.ShouldBeTrue();
        return message;
    }
}
