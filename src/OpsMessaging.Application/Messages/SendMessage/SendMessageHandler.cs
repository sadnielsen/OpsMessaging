using OpsMessaging.Application.Abstractions;
using OpsMessaging.Domain.Common;
using OpsMessaging.Domain.Messages;

namespace OpsMessaging.Application.Messages.SendMessage;

/// <summary>
/// Use case "send a message": validate, resolve the audience, dispatch per recipient and channel,
/// store, and publish the <see cref="MessageDispatched"/> event.
/// </summary>
/// <remarks>
/// CONCEPT (Clean Architecture): A use case orchestrates: it holds no business rules itself (those live
/// in the domain) and no technology (that lives behind the ports). That makes it readable as a recipe
/// and testable with simple fakes.
/// </remarks>
public sealed class SendMessageHandler(
    IMessageRepository repository,
    IRecipientDirectory recipientDirectory,
    IChannelDispatcher channelDispatcher,
    IEventPublisher eventPublisher,
    IClock clock,
    ICurrentUser currentUser)
{
    /// <summary>Sends the message described by <paramref name="command"/>.</summary>
    public async Task<Result<SendMessageResult>> HandleAsync(SendMessageCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        // One "now" for the whole use case, so all rules see the same moment.
        var now = clock.UtcNow;

        // 1. Validate: turn plain input into domain objects. Each Create enforces its own rules.
        var title = MessageTitle.Create(command.Title);           // BR-01
        if (title.IsFailure)
        {
            return Fail(title);
        }

        var body = MessageBody.Create(command.Body);              // BR-01
        if (body.IsFailure)
        {
            return Fail(body);
        }

        var audience = Audience.Create(command.RoleGroups, command.RegionCode); // BR-02
        if (audience.IsFailure)
        {
            return Fail(audience);
        }

        var created = Message.Create(                             // BR-03, BR-04
            title.Value,
            body.Value,
            command.Priority,
            command.Channels,
            audience.Value,
            command.ValidUntil,
            currentUser.SenderId,
            now);
        if (created.IsFailure)
        {
            return Fail(created);
        }

        var message = created.Value;

        // 2. Resolve the audience to concrete (pseudonymous) recipients.
        var recipients = await recipientDirectory.FindRecipientsAsync(message.Audience, cancellationToken);

        // 3. BR-05: nobody in the audience -> stop here. Nothing is sent, stored or published.
        var dispatched = message.Dispatch(recipients, now);
        if (dispatched.IsFailure)
        {
            return Fail(dispatched);
        }

        // 4. Send every delivery and record the outcome on the aggregate.
        foreach (var delivery in message.Deliveries)
        {
            var accepted = await channelDispatcher.TrySendAsync(
                message, delivery.RecipientId, delivery.Channel, cancellationToken);

            if (accepted)
            {
                message.MarkDeliverySent(delivery.RecipientId, delivery.Channel);
            }
            else
            {
                message.MarkDeliveryFailed(delivery.RecipientId, delivery.Channel);
            }
        }

        // 5. Store the message.
        await repository.AddAsync(message, cancellationToken);

        // 6. Publish the domain events.
        // CONCEPT (Event-driven): Storing (step 5) and publishing (step 6) are two separate actions.
        // If the process crashes in between, the message is stored but the event is lost; if we
        // published first, a failed save would announce a message that does not exist.
        // REAL-IMPLEMENTATION: Use the outbox pattern — store the event in an Outbox table in the SAME
        // database transaction as the message, and let a background worker publish it to Azure
        // Service Bus (at-least-once). Consumers then deduplicate on MessageId.
        // In production, step 4 would also move behind a queue so a slow SMS provider cannot block
        // the HTTP request.
        foreach (var domainEvent in message.DomainEvents)
        {
            await eventPublisher.PublishAsync(domainEvent, cancellationToken);
        }

        message.ClearDomainEvents();

        return Result.Success(new SendMessageResult(
            message.Id,
            message.Status,
            message.Deliveries.Select(d => d.RecipientId).Distinct().Count(),
            message.Deliveries.Count));
    }

    private static Result<SendMessageResult> Fail(Result failed) => Result.Failure<SendMessageResult>(failed.Error!);
}
