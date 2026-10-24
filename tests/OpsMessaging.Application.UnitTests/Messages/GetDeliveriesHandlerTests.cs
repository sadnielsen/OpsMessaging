using OpsMessaging.Application.Messages;
using OpsMessaging.Application.Messages.GetDeliveries;
using OpsMessaging.Domain.Messages;

namespace OpsMessaging.Application.UnitTests.Messages;

public sealed class GetDeliveriesHandlerTests
{
    private readonly MessageScenario _scenario = new();

    private GetDeliveriesHandler CreateHandler() => new(_scenario.Repository);

    [Fact]
    public async Task HandleAsync_ExistingMessage_ReturnsOneDeliveryPerRecipientPerChannel()
    {
        var id = await _scenario.SendMessageAsync(TimeSpan.FromHours(2));

        var result = await CreateHandler().HandleAsync(new GetDeliveriesQuery(id), TestContext.Current.CancellationToken);

        result.ShouldSucceed();
        result.Value.Count.ShouldBe(4);
        result.Value.Select(d => d.RecipientId).Distinct().ShouldBe(["EMP-0001", "EMP-0002"], ignoreOrder: true);
        result.Value.ShouldAllBe(d => d.Status == DeliveryStatus.Sent);
    }

    [Fact]
    public async Task HandleAsync_UnknownId_FailsWithNotFound()
    {
        var result = await CreateHandler().HandleAsync(new GetDeliveriesQuery(Guid.NewGuid()), TestContext.Current.CancellationToken);

        result.ShouldFailWith(ApplicationErrors.NotFoundCode);
    }
}
