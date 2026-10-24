using OpsMessaging.Domain.Common;
using OpsMessaging.Domain.Messages;

namespace OpsMessaging.Domain.UnitTests.Messages;

public sealed class MessageCreateTests
{
    [Fact]
    public void Create_ValidInput_HasStatusCreatedAndNoDeliveries()
    {
        var message = TestData.CreateMessage().Value;

        message.Status.ShouldBe(MessageStatus.Created);
        message.CreatedAt.ShouldBe(TestData.Now);
        message.Deliveries.ShouldBeEmpty();
        message.DomainEvents.ShouldBeEmpty();
    }

    // --- BR-03: an urgent message goes via at least two channels ---

    [Fact]
    public void Create_UrgentWithTwoChannels_PassesBR03()
    {
        var result = TestData.CreateMessage(Priority.Urgent, [Channel.Push, Channel.Sms]);

        result.ShouldSucceed();
    }

    [Fact]
    public void Create_UrgentWithOneChannel_FailsWithBR03()
    {
        var result = TestData.CreateMessage(Priority.Urgent, [Channel.Push]);

        result.ShouldFailWith("BR-03");
    }

    [Fact]
    public void Create_UrgentWithSameChannelTwice_FailsWithBR03()
    {
        // The same channel twice is no redundancy.
        var result = TestData.CreateMessage(Priority.Urgent, [Channel.Push, Channel.Push]);

        result.ShouldFailWith("BR-03");
    }

    [Fact]
    public void Create_NormalWithOneChannel_PassesBR03()
    {
        var result = TestData.CreateMessage(Priority.Normal, [Channel.Email]);

        result.ShouldSucceed();
    }

    [Fact]
    public void Create_WithoutChannels_FailsWithBR03()
    {
        var result = TestData.CreateMessage(Priority.Normal, []);

        result.ShouldFailWith("BR-03");
    }

    [Fact]
    public void Create_DuplicateChannels_AreStoredOnce()
    {
        var message = TestData.CreateMessage(Priority.Normal, [Channel.Sms, Channel.Sms]).Value;

        message.Channels.ShouldBe([Channel.Sms]);
    }

    [Fact]
    public void Create_UnknownChannel_FailsWithValidationError()
    {
        var result = TestData.CreateMessage(Priority.Normal, [(Channel)99]);

        result.ShouldFailWith(Error.ValidationCode);
    }

    [Fact]
    public void Create_UnknownPriority_FailsWithValidationError()
    {
        var result = TestData.CreateMessage((Priority)99);

        result.ShouldFailWith(Error.ValidationCode);
    }

    // --- BR-04: ValidUntil lies in the future and at most 24 hours after now ---

    [Fact]
    public void Create_ValidUntilExactly24HoursAhead_PassesBR04()
    {
        var result = TestData.CreateMessage(validUntil: TestData.Now.AddHours(24));

        result.ShouldSucceed();
    }

    [Fact]
    public void Create_ValidUntilOneMinuteAhead_PassesBR04()
    {
        var result = TestData.CreateMessage(validUntil: TestData.Now.AddMinutes(1));

        result.ShouldSucceed();
    }

    [Fact]
    public void Create_ValidUntilNow_FailsWithBR04()
    {
        var result = TestData.CreateMessage(validUntil: TestData.Now);

        result.ShouldFailWith("BR-04");
    }

    [Fact]
    public void Create_ValidUntilInThePast_FailsWithBR04()
    {
        var result = TestData.CreateMessage(validUntil: TestData.Now.AddMinutes(-1));

        result.ShouldFailWith("BR-04");
    }

    [Fact]
    public void Create_ValidUntilMoreThan24HoursAhead_FailsWithBR04()
    {
        var result = TestData.CreateMessage(validUntil: TestData.Now.AddHours(24).AddSeconds(1));

        result.ShouldFailWith("BR-04");
    }
}
