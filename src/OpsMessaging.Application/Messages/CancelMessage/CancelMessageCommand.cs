namespace OpsMessaging.Application.Messages.CancelMessage;

/// <summary>Input for <see cref="CancelMessageHandler"/>.</summary>
/// <param name="MessageId">The message to cancel.</param>
public sealed record CancelMessageCommand(Guid MessageId);
