using OpsMessaging.Application.Abstractions;
using OpsMessaging.Domain.Common;

namespace OpsMessaging.Application.Messages.GetMessage;

/// <summary>Use case "read a message".</summary>
public sealed class GetMessageHandler(IMessageRepository repository, IClock clock)
{
    /// <summary>Returns the message, or a NOT_FOUND error.</summary>
    public async Task<Result<MessageDetails>> HandleAsync(GetMessageQuery query, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);

        var message = await repository.GetByIdAsync(query.MessageId, cancellationToken);
        if (message is null)
        {
            return Result.Failure<MessageDetails>(ApplicationErrors.MessageNotFound(query.MessageId));
        }

        return Result.Success(new MessageDetails(
            message.Id,
            message.Title.Value,
            message.Body.Value,
            message.Priority,
            message.Channels,
            message.Audience.RoleGroups,
            message.Audience.RegionCode,
            message.ValidUntil,
            message.GetEffectiveStatus(clock.UtcNow),
            message.CreatedAt,
            message.CancelledAt,
            message.Deliveries.Select(d => d.RecipientId).Distinct().Count(),
            message.Deliveries.Count));
    }
}
