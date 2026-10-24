using OpsMessaging.Application.Messages;
using OpsMessaging.Application.Messages.CancelMessage;
using OpsMessaging.Domain.Common;
using OpsMessaging.Domain.Messages;

namespace OpsMessaging.Application.UnitTests.Messages;

public sealed class CancelMessageHandlerTests
{
    private readonly MessageScenario _scenario = new();

    private CancelMessageHandler CreateHandler() => new(_scenario.Repository, _scenario.Clock);

    private Task<Result> CancelAsync(Guid id) =>
        CreateHandler().HandleAsync(new CancelMessageCommand(id), TestContext.Current.CancellationToken);

    [Fact]
    public async Task HandleAsync_DispatchedMessage_IsCancelledAndStored()
    {
        var id = await _scenario.SendMessageAsync(TimeSpan.FromHours(2));

        var result = await CancelAsync(id);

        result.ShouldSucceed();
        _scenario.Repository.Updated.ShouldHaveSingleItem().Status.ShouldBe(MessageStatus.Cancelled);
    }

    [Fact]
    public async Task HandleAsync_AlreadyCancelled_FailsWithBR06AndStoresNothing()
    {
        var id = await _scenario.SendMessageAsync(TimeSpan.FromHours(2));
        await CancelAsync(id);

        var result = await CancelAsync(id);

        result.ShouldFailWith("BR-06");
        _scenario.Repository.Updated.Count.ShouldBe(1); // only the first cancel
    }

    [Fact]
    public async Task HandleAsync_ExpiredMessage_FailsWithBR07()
    {
        var id = await _scenario.SendMessageAsync(TimeSpan.FromHours(2));
        _scenario.Clock.UtcNow = _scenario.Clock.UtcNow.AddHours(3);

        var result = await CancelAsync(id);

        result.ShouldFailWith("BR-07");
        _scenario.Repository.Updated.ShouldBeEmpty();
    }

    [Fact]
    public async Task HandleAsync_UnknownId_FailsWithNotFound()
    {
        var result = await CancelAsync(Guid.NewGuid());

        result.ShouldFailWith(ApplicationErrors.NotFoundCode);
    }
}
