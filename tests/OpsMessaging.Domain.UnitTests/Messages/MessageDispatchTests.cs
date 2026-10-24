using OpsMessaging.Domain.Messages;

namespace OpsMessaging.Domain.UnitTests.Messages;

public sealed class MessageDispatchTests
{
    // --- BR-05: zero recipients means the message is not sent ---

    [Fact]
    public void Dispatch_WithRecipients_PassesBR05()
    {
        var message = TestData.CreateMessage(Priority.Urgent, [Channel.Push, Channel.Sms]).Value;
        var dispatchedAt = TestData.Now.AddSeconds(1);

        var result = message.Dispatch(
            [TestData.Recipient("EMP-0001"), TestData.Recipient("EMP-0002"), TestData.Recipient("EMP-0003")],
            dispatchedAt);

        result.ShouldSucceed();
        message.Status.ShouldBe(MessageStatus.Dispatched);
        message.DispatchedAt.ShouldBe(dispatchedAt);
    }

    [Fact]
    public void Dispatch_WithoutRecipients_FailsWithBR05()
    {
        var message = TestData.CreateMessage().Value;

        var result = message.Dispatch([], TestData.Now);

        result.ShouldFailWith("BR-05");
    }

    [Fact]
    public void Dispatch_WithoutRecipients_CreatesNoDeliveriesAndNoEventBR05()
    {
        var message = TestData.CreateMessage().Value;

        message.Dispatch([], TestData.Now);

        message.Status.ShouldBe(MessageStatus.Created);
        message.Deliveries.ShouldBeEmpty();
        message.DomainEvents.ShouldBeEmpty();
    }

    // --- Deliveries and the domain event ---

    [Fact]
    public void Dispatch_ThreeRecipientsTwoChannels_CreatesSixPendingDeliveries()
    {
        var message = TestData.CreateMessage(Priority.Urgent, [Channel.Push, Channel.Sms]).Value;

        message.Dispatch(
            [TestData.Recipient("EMP-0001"), TestData.Recipient("EMP-0002"), TestData.Recipient("EMP-0003")],
            TestData.Now);

        message.Deliveries.Count.ShouldBe(6);
        message.Deliveries.ShouldAllBe(d => d.Status == DeliveryStatus.Pending);
    }

    [Fact]
    public void Dispatch_SameRecipientTwice_CreatesOneDeliveryPerChannel()
    {
        var message = TestData.CreateMessage(Priority.Normal, [Channel.Push]).Value;

        message.Dispatch([TestData.Recipient("EMP-0001"), TestData.Recipient("EMP-0001")], TestData.Now);

        message.Deliveries.Count.ShouldBe(1);
    }

    [Fact]
    public void Dispatch_WithRecipients_RaisesMessageDispatchedWithCounts()
    {
        // CONCEPT (Event-driven): The aggregate records the event; publishing it is done later
        // by the Application layer.
        var message = TestData.CreateMessage(Priority.Urgent, [Channel.Push, Channel.Sms]).Value;

        message.Dispatch([TestData.Recipient("EMP-0001"), TestData.Recipient("EMP-0002")], TestData.Now);

        var dispatched = message.DomainEvents.ShouldHaveSingleItem().ShouldBeOfType<MessageDispatched>();
        dispatched.MessageId.ShouldBe(message.Id);
        dispatched.SenderId.ShouldBe(TestData.Sender);
        dispatched.Priority.ShouldBe(Priority.Urgent);
        dispatched.Channels.ShouldBe([Channel.Push, Channel.Sms]);
        dispatched.RecipientCount.ShouldBe(2);
        dispatched.DeliveryCount.ShouldBe(4);
        dispatched.OccurredAt.ShouldBe(TestData.Now);
    }

    [Fact]
    public void ClearDomainEvents_AfterDispatch_RemovesEvents()
    {
        var message = TestData.DispatchedMessage();

        message.ClearDomainEvents();

        message.DomainEvents.ShouldBeEmpty();
    }

    [Fact]
    public void Dispatch_AlreadyDispatched_Throws()
    {
        // Dispatching twice is a programming error, not a business rule: exception, not Result.
        var message = TestData.DispatchedMessage();

        Should.Throw<InvalidOperationException>(() =>
            message.Dispatch([TestData.Recipient("EMP-0003")], TestData.Now));
    }

    // --- Delivery status: Pending -> Sent or Pending -> Failed ---

    [Fact]
    public void MarkDeliverySent_PendingDelivery_BecomesSent()
    {
        var message = TestData.DispatchedMessage();
        var recipient = TestData.Recipient("EMP-0001");

        message.MarkDeliverySent(recipient, Channel.Push);

        message.Deliveries.Single(d => d.RecipientId == recipient && d.Channel == Channel.Push)
            .Status.ShouldBe(DeliveryStatus.Sent);
    }

    [Fact]
    public void MarkDeliveryFailed_PendingDelivery_BecomesFailed()
    {
        var message = TestData.DispatchedMessage();
        var recipient = TestData.Recipient("EMP-0002");

        message.MarkDeliveryFailed(recipient, Channel.Sms);

        message.Deliveries.Single(d => d.RecipientId == recipient && d.Channel == Channel.Sms)
            .Status.ShouldBe(DeliveryStatus.Failed);
    }

    [Fact]
    public void MarkDeliverySent_AlreadySent_Throws()
    {
        var message = TestData.DispatchedMessage();
        var recipient = TestData.Recipient("EMP-0001");
        message.MarkDeliverySent(recipient, Channel.Push);

        Should.Throw<InvalidOperationException>(() => message.MarkDeliveryFailed(recipient, Channel.Push));
    }

    [Fact]
    public void MarkDeliverySent_UnknownDelivery_Throws()
    {
        var message = TestData.DispatchedMessage();

        Should.Throw<InvalidOperationException>(() =>
            message.MarkDeliverySent(TestData.Recipient("EMP-9999"), Channel.Push));
    }
}
