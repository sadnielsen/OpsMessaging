using OpsMessaging.Application.Abstractions;
using OpsMessaging.Domain.Common;

namespace OpsMessaging.Application.Messages.GetDeliveries;

/// <summary>Use case "show the delivery status per recipient per channel".</summary>
public sealed class GetDeliveriesHandler(IMessageRepository repository)
{
    /// <summary>Returns the deliveries of the message, or a NOT_FOUND error.</summary>
    public async Task<Result<IReadOnlyList<DeliveryDetails>>> HandleAsync(
        GetDeliveriesQuery query, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);

        var message = await repository.GetByIdAsync(query.MessageId, cancellationToken);
        if (message is null)
        {
            return Result.Failure<IReadOnlyList<DeliveryDetails>>(ApplicationErrors.MessageNotFound(query.MessageId));
        }

        IReadOnlyList<DeliveryDetails> deliveries = message.Deliveries
            .Select(d => new DeliveryDetails(d.RecipientId.Value, d.Channel, d.Status))
            .ToList();

        return Result.Success(deliveries);
    }
}
