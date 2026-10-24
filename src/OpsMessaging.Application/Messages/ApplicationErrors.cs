using OpsMessaging.Domain.Common;

namespace OpsMessaging.Application.Messages;

/// <summary>Expected errors that come from the use cases rather than from a business rule.</summary>
public static class ApplicationErrors
{
    /// <summary>Code for "the requested message does not exist" (the Api maps it to 404).</summary>
    public const string NotFoundCode = "NOT_FOUND";

    /// <summary>The message with this ID does not exist.</summary>
    public static Error MessageNotFound(Guid id) => new(NotFoundCode, $"Message {id} was not found.");
}
