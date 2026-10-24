using OpsMessaging.Domain.Messages;

namespace OpsMessaging.Application.Abstractions;

/// <summary>Resolves an <see cref="Audience"/> to the employees who belong to it.</summary>
/// <remarks>
/// CONCEPT (Clean Architecture): A port to another system (the staff/roster service). The use case only
/// asks "who is in this audience?"; how the answer is obtained is an Infrastructure concern.
/// CONCEPT (Privacy-by-design): The answer contains pseudonymous IDs only, never names or phone numbers.
/// </remarks>
public interface IRecipientDirectory
{
    /// <summary>Returns the recipients that match the audience; an empty list if nobody matches.</summary>
    Task<IReadOnlyList<RecipientId>> FindRecipientsAsync(Audience audience, CancellationToken cancellationToken);
}
