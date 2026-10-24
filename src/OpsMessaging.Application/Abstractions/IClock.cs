namespace OpsMessaging.Application.Abstractions;

/// <summary>Provides the current time.</summary>
/// <remarks>
/// CONCEPT (Unit test): Time is an external dependency, just like a database. Behind a port, tests can
/// use a fixed time and check rules such as BR-04 and BR-07 deterministically.
/// </remarks>
public interface IClock
{
    /// <summary>The current time in UTC.</summary>
    DateTimeOffset UtcNow { get; }
}
