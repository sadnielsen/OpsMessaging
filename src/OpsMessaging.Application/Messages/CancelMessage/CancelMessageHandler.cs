using OpsMessaging.Application.Abstractions;
using OpsMessaging.Domain.Common;

namespace OpsMessaging.Application.Messages.CancelMessage;

/// <summary>Use case "cancel (withdraw) a message".</summary>
public sealed class CancelMessageHandler(IMessageRepository repository, IClock clock)
{
    /// <summary>Cancels the message; fails with NOT_FOUND, BR-06 or BR-07.</summary>
    public async Task<Result> HandleAsync(CancelMessageCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        var message = await repository.GetByIdAsync(command.MessageId, cancellationToken);
        if (message is null)
        {
            return Result.Failure(ApplicationErrors.MessageNotFound(command.MessageId));
        }

        // BR-06 and BR-07 are checked by the aggregate, not here.
        var cancelled = message.Cancel(clock.UtcNow);
        if (cancelled.IsFailure)
        {
            return cancelled;
        }

        await repository.UpdateAsync(message, cancellationToken);
        return Result.Success();
    }
}
