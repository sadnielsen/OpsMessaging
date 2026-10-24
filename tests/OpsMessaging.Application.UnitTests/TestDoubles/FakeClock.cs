using OpsMessaging.Application.Abstractions;

namespace OpsMessaging.Application.UnitTests.TestDoubles;

// CONCEPT (Unit test): Hand-written fakes instead of a mocking library. A fake is a tiny, working
// implementation of a port: easy to read, and the test describes behaviour instead of method calls.
internal sealed class FakeClock : IClock
{
    public DateTimeOffset UtcNow { get; set; } = new(2026, 10, 24, 12, 0, 0, TimeSpan.Zero);
}
