using OpsMessaging.Domain.Messages;

namespace OpsMessaging.Application.Abstractions;

/// <summary>The authenticated caller (user or application) of the current request.</summary>
/// <remarks>
/// CONCEPT (OAuth2/OIDC): The use case does not read HTTP headers or tokens. The adapter reads the
/// <c>oid</c> claim from the validated access token and hands over only the ID.
/// </remarks>
public interface ICurrentUser
{
    /// <summary>The object ID of the caller; becomes the <see cref="SenderId"/> of a message.</summary>
    SenderId SenderId { get; }
}
