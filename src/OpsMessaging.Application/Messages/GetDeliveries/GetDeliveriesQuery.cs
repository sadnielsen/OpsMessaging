namespace OpsMessaging.Application.Messages.GetDeliveries;

/// <summary>Input for <see cref="GetDeliveriesHandler"/>.</summary>
/// <param name="MessageId">The message whose deliveries to read.</param>
public sealed record GetDeliveriesQuery(Guid MessageId);
