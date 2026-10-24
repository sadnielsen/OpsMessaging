using OpsMessaging.Domain.Messages;

namespace OpsMessaging.Application.Abstractions;

/// <summary>Stores and loads <see cref="Message"/> aggregates.</summary>
/// <remarks>
/// CONCEPT (Clean Architecture): This interface is a "port". The Application layer defines WHAT it
/// needs (store a message, find it again); Infrastructure decides HOW (in memory now, a database later).
/// The use cases never know about SQL.
/// </remarks>
public interface IMessageRepository
{
    /// <summary>Stores a new message.</summary>
    Task AddAsync(Message message, CancellationToken cancellationToken);

    /// <summary>Returns the message with this ID, or <c>null</c> if it does not exist.</summary>
    Task<Message?> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    /// <summary>Stores the changes to an existing message (for example after cancelling it).</summary>
    Task UpdateAsync(Message message, CancellationToken cancellationToken);
}
