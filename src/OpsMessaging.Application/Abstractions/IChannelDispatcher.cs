using OpsMessaging.Domain.Messages;

namespace OpsMessaging.Application.Abstractions;

/// <summary>Sends a message to one recipient via one channel (push, SMS, e-mail).</summary>
/// <remarks>
/// CONCEPT (Clean Architecture): A port. The adapter behind it looks up the contact details of the
/// recipient (phone number, push token) at the moment of sending, so they never enter the domain.
/// CONCEPT (Resilience): A failed delivery is an expected outcome, reported as <c>false</c>, not as an
/// exception. Retries and fallbacks belong in the adapter.
/// </remarks>
public interface IChannelDispatcher
{
    /// <summary>Tries to send the message; returns <c>true</c> when the channel accepted it.</summary>
    Task<bool> TrySendAsync(Message message, RecipientId recipientId, Channel channel, CancellationToken cancellationToken);
}
