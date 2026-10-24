using OpsMessaging.Application.Messages.SendMessage;
using OpsMessaging.Application.UnitTests.TestDoubles;
using OpsMessaging.Domain.Common;
using OpsMessaging.Domain.Messages;

namespace OpsMessaging.Application.UnitTests.Messages;

public sealed class SendMessageHandlerTests
{
    private readonly FakeMessageRepository _repository = new();
    private readonly FakeRecipientDirectory _directory = new FakeRecipientDirectory()
        .WithRecipients("EMP-0001", "EMP-0002", "EMP-0003");
    private readonly RecordingChannelDispatcher _dispatcher = new();
    private readonly RecordingEventPublisher _publisher = new();
    private readonly FakeClock _clock = new();
    private readonly FakeCurrentUser _currentUser = new();

    private SendMessageHandler CreateHandler() =>
        new(_repository, _directory, _dispatcher, _publisher, _clock, _currentUser);

    private SendMessageCommand ValidUrgentCommand() => new(
        Title: "Switch failure sector 4",
        Body: "Expect delays. Follow the instructions of traffic control.",
        Priority: Priority.Urgent,
        Channels: [Channel.Push, Channel.Sms],
        RoleGroups: ["Driver", "Conductor"],
        RegionCode: "REGION-NORTH",
        ValidUntil: _clock.UtcNow.AddHours(4));

    private Task<Result<SendMessageResult>> SendAsync(SendMessageCommand command) =>
        CreateHandler().HandleAsync(command, TestContext.Current.CancellationToken);

    // --- Happy flow ---

    [Fact]
    public async Task HandleAsync_ValidCommand_ReturnsDispatchedWithCounts()
    {
        var result = await SendAsync(ValidUrgentCommand());

        result.ShouldSucceed();
        result.Value.Status.ShouldBe(MessageStatus.Dispatched);
        result.Value.RecipientCount.ShouldBe(3);
        result.Value.DeliveryCount.ShouldBe(6); // 3 recipients × 2 channels
    }

    [Fact]
    public async Task HandleAsync_ValidCommand_ResolvesTheRequestedAudience()
    {
        await SendAsync(ValidUrgentCommand());

        _directory.LastAudience.ShouldNotBeNull();
        _directory.LastAudience.RoleGroups.ShouldBe(["Conductor", "Driver"]);
        _directory.LastAudience.RegionCode.ShouldBe("REGION-NORTH");
    }

    [Fact]
    public async Task HandleAsync_ValidCommand_SendsEveryRecipientViaEveryChannel()
    {
        await SendAsync(ValidUrgentCommand());

        _dispatcher.Attempts.Count.ShouldBe(6);
        _dispatcher.Attempts.Select(a => a.Channel).Distinct().ShouldBe([Channel.Push, Channel.Sms], ignoreOrder: true);
    }

    [Fact]
    public async Task HandleAsync_ValidCommand_StoresMessageWithSenderFromToken()
    {
        var result = await SendAsync(ValidUrgentCommand());

        var stored = _repository.Added.ShouldHaveSingleItem();
        stored.Id.ShouldBe(result.Value.Id);
        stored.SenderId.ShouldBe(_currentUser.SenderId);
    }

    [Fact]
    public async Task HandleAsync_ValidCommand_PublishesMessageDispatchedOnce()
    {
        var result = await SendAsync(ValidUrgentCommand());

        var published = _publisher.Published.ShouldHaveSingleItem().ShouldBeOfType<MessageDispatched>();
        published.MessageId.ShouldBe(result.Value.Id);
        published.DeliveryCount.ShouldBe(6);
        _repository.Added.Single().DomainEvents.ShouldBeEmpty(); // cleared after publishing
    }

    [Fact]
    public async Task HandleAsync_OneChannelFails_MarksOnlyThoseDeliveriesFailed()
    {
        _dispatcher.FailingChannels.Add(Channel.Sms);

        await SendAsync(ValidUrgentCommand());

        var deliveries = _repository.Added.Single().Deliveries;
        deliveries.Where(d => d.Channel == Channel.Sms).ShouldAllBe(d => d.Status == DeliveryStatus.Failed);
        deliveries.Where(d => d.Channel == Channel.Push).ShouldAllBe(d => d.Status == DeliveryStatus.Sent);
    }

    // --- BR-05: the key test of this phase ---

    [Fact]
    public async Task HandleAsync_AudienceWithoutRecipients_FailsWithBR05AndDoesNothing()
    {
        // CONCEPT (Unit test): Fakes make "nothing happened" provable: no send attempt, nothing stored,
        // no event. With a real SMS provider and broker this would be hard to check.
        _directory.Recipients.Clear();

        var result = await SendAsync(ValidUrgentCommand());

        result.ShouldFailWith("BR-05");
        _dispatcher.Attempts.ShouldBeEmpty();
        _publisher.Published.ShouldBeEmpty();
        _repository.Added.ShouldBeEmpty();
    }

    // --- Validation failures stop before any side effect ---

    [Fact]
    public async Task HandleAsync_EmptyTitle_FailsWithBR01BeforeResolvingAudience()
    {
        var result = await SendAsync(ValidUrgentCommand() with { Title = "" });

        result.ShouldFailWith("BR-01");
        _directory.WasCalled.ShouldBeFalse();
    }

    [Fact]
    public async Task HandleAsync_BodyTooLong_FailsWithBR01()
    {
        var result = await SendAsync(ValidUrgentCommand() with { Body = new string('a', MessageBody.MaxLength + 1) });

        result.ShouldFailWith("BR-01");
    }

    [Fact]
    public async Task HandleAsync_NoRoleGroups_FailsWithBR02()
    {
        var result = await SendAsync(ValidUrgentCommand() with { RoleGroups = [] });

        result.ShouldFailWith("BR-02");
        _directory.WasCalled.ShouldBeFalse();
    }

    [Fact]
    public async Task HandleAsync_UrgentWithOneChannel_FailsWithBR03()
    {
        var result = await SendAsync(ValidUrgentCommand() with { Channels = [Channel.Push] });

        result.ShouldFailWith("BR-03");
        _dispatcher.Attempts.ShouldBeEmpty();
    }

    [Fact]
    public async Task HandleAsync_ValidUntilMoreThan24HoursAhead_FailsWithBR04()
    {
        // The rule is checked against the injected clock, not the machine clock.
        var result = await SendAsync(ValidUrgentCommand() with { ValidUntil = _clock.UtcNow.AddHours(25) });

        result.ShouldFailWith("BR-04");
        _repository.Added.ShouldBeEmpty();
    }
}
