using OpsMessaging.Domain.Messages;

namespace OpsMessaging.Domain.UnitTests.Messages;

public sealed class MessageCancelTests
{
    // --- BR-06: only a dispatched message can be cancelled ---

    [Fact]
    public void Cancel_DispatchedMessage_PassesBR06()
    {
        var message = TestData.DispatchedMessage();
        var cancelledAt = TestData.Now.AddMinutes(5);

        var result = message.Cancel(cancelledAt);

        result.ShouldSucceed();
        message.Status.ShouldBe(MessageStatus.Cancelled);
        message.CancelledAt.ShouldBe(cancelledAt);
    }

    [Fact]
    public void Cancel_NotYetDispatched_FailsWithBR06()
    {
        var message = TestData.CreateMessage().Value;

        var result = message.Cancel(TestData.Now);

        result.ShouldFailWith("BR-06");
        message.Status.ShouldBe(MessageStatus.Created);
    }

    [Fact]
    public void Cancel_AlreadyCancelled_FailsWithBR06()
    {
        var message = TestData.DispatchedMessage();
        message.Cancel(TestData.Now).ShouldSucceed();

        var result = message.Cancel(TestData.Now.AddMinutes(1));

        result.ShouldFailWith("BR-06");
    }

    // --- BR-07: an expired message can no longer be cancelled ---

    [Fact]
    public void Cancel_AtExactlyValidUntil_PassesBR07()
    {
        // "Expired" means ValidUntil < now, so at exactly ValidUntil the message is still valid.
        var validUntil = TestData.Now.AddHours(1);
        var message = TestData.DispatchedMessage(validUntil);

        var result = message.Cancel(validUntil);

        result.ShouldSucceed();
    }

    [Fact]
    public void Cancel_AfterValidUntil_FailsWithBR07()
    {
        var validUntil = TestData.Now.AddHours(1);
        var message = TestData.DispatchedMessage(validUntil);

        var result = message.Cancel(validUntil.AddSeconds(1));

        result.ShouldFailWith("BR-07");
        message.Status.ShouldBe(MessageStatus.Dispatched);
    }

    // --- Derived status "Expired" (DESIGN.md §1.3) ---

    [Fact]
    public void GetEffectiveStatus_DispatchedAndPastValidUntil_IsExpired()
    {
        var validUntil = TestData.Now.AddHours(1);
        var message = TestData.DispatchedMessage(validUntil);

        message.GetEffectiveStatus(validUntil.AddSeconds(1)).ShouldBe(MessageStatus.Expired);
        message.Status.ShouldBe(MessageStatus.Dispatched); // stored status is unchanged
    }

    [Fact]
    public void GetEffectiveStatus_DispatchedAndStillValid_IsDispatched()
    {
        var message = TestData.DispatchedMessage(TestData.Now.AddHours(1));

        message.GetEffectiveStatus(TestData.Now.AddMinutes(30)).ShouldBe(MessageStatus.Dispatched);
    }

    [Fact]
    public void GetEffectiveStatus_CancelledAndPastValidUntil_StaysCancelled()
    {
        var validUntil = TestData.Now.AddHours(1);
        var message = TestData.DispatchedMessage(validUntil);
        message.Cancel(TestData.Now).ShouldSucceed();

        message.GetEffectiveStatus(validUntil.AddHours(1)).ShouldBe(MessageStatus.Cancelled);
    }
}
