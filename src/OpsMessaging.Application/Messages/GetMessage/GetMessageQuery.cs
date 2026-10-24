namespace OpsMessaging.Application.Messages.GetMessage;

/// <summary>Input for <see cref="GetMessageHandler"/>.</summary>
/// <param name="MessageId">The message to read.</param>
public sealed record GetMessageQuery(Guid MessageId);
