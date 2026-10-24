using OpsMessaging.Application.Messages;
using OpsMessaging.Application.Messages.GetMessage;
using OpsMessaging.Domain.Messages;

namespace OpsMessaging.Application.UnitTests.Messages;

public sealed class GetMessageHandlerTests
{
    private readonly MessageScenario _scenario = new();

    private GetMessageHandler CreateHandler() => new(_scenario.Repository, _scenario.Clock);

    [Fact]
    public async Task HandleAsync_ExistingMessage_ReturnsDetails()
    {
        var id = await _scenario.SendMessageAsync(TimeSpan.FromHours(2));

        var result = await CreateHandler().HandleAsync(new GetMessageQuery(id), TestContext.Current.CancellationToken);

        result.ShouldSucceed();
        result.Value.Id.ShouldBe(id);
        result.Value.Title.ShouldBe("Shift change");
        result.Value.Status.ShouldBe(MessageStatus.Dispatched);
        result.Value.RecipientCount.ShouldBe(2);
        result.Value.DeliveryCount.ShouldBe(4);
    }

    [Fact]
    public async Task HandleAsync_AfterValidUntil_ReportsExpired()
    {
        var id = await _scenario.SendMessageAsync(TimeSpan.FromHours(2));
        _scenario.Clock.UtcNow = _scenario.Clock.UtcNow.AddHours(3);

        var result = await CreateHandler().HandleAsync(new GetMessageQuery(id), TestContext.Current.CancellationToken);

        result.Value.Status.ShouldBe(MessageStatus.Expired);
    }

    [Fact]
    public async Task HandleAsync_UnknownId_FailsWithNotFound()
    {
        var result = await CreateHandler().HandleAsync(new GetMessageQuery(Guid.NewGuid()), TestContext.Current.CancellationToken);

        result.ShouldFailWith(ApplicationErrors.NotFoundCode);
    }
}
