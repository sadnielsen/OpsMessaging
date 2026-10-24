using OpsMessaging.Domain.Messages;

namespace OpsMessaging.Application.Messages.SendMessage;

/// <summary>Input for <see cref="SendMessageHandler"/>: everything a dispatcher fills in.</summary>
/// <remarks>
/// Plain values on purpose: the Api maps its request DTO to this command, and the handler turns the
/// values into validated domain objects. The sender is not part of the command: it comes from the
/// access token (<see cref="Abstractions.ICurrentUser"/>), so a caller cannot send "as someone else".
/// </remarks>
/// <param name="Title">Message title (BR-01).</param>
/// <param name="Body">Message body (BR-01).</param>
/// <param name="Priority">Normal or urgent (BR-03).</param>
/// <param name="Channels">Requested channels (BR-03).</param>
/// <param name="RoleGroups">Role groups of the audience (BR-02).</param>
/// <param name="RegionCode">Optional region of the audience.</param>
/// <param name="ValidUntil">Until when the message is relevant (BR-04).</param>
public sealed record SendMessageCommand(
    string Title,
    string Body,
    Priority Priority,
    IReadOnlyList<Channel> Channels,
    IReadOnlyList<string> RoleGroups,
    string? RegionCode,
    DateTimeOffset ValidUntil);
