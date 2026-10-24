using OpsMessaging.Domain.Messages;

namespace OpsMessaging.Application.Messages.GetDeliveries;

/// <summary>Read model of one delivery.</summary>
/// <remarks>
/// CONCEPT (Privacy-by-design): Pseudonymous recipient ID, channel and status. No phone number,
/// e-mail address or push token: the domain does not even have them.
/// </remarks>
/// <param name="RecipientId">Pseudonymous recipient ID.</param>
/// <param name="Channel">Channel used.</param>
/// <param name="Status">Pending, Sent or Failed.</param>
public sealed record DeliveryDetails(string RecipientId, Channel Channel, DeliveryStatus Status);
