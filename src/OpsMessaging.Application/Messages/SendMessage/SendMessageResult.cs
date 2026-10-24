using OpsMessaging.Domain.Messages;

namespace OpsMessaging.Application.Messages.SendMessage;

/// <summary>Outcome of a successful send: IDs and counts only.</summary>
/// <param name="Id">ID of the new message.</param>
/// <param name="Status">Status after sending (Dispatched).</param>
/// <param name="RecipientCount">Number of recipients the audience resolved to.</param>
/// <param name="DeliveryCount">Number of deliveries (recipients × channels).</param>
public sealed record SendMessageResult(Guid Id, MessageStatus Status, int RecipientCount, int DeliveryCount);
