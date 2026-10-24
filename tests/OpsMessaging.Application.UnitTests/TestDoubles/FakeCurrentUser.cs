using OpsMessaging.Application.Abstractions;
using OpsMessaging.Domain.Messages;

namespace OpsMessaging.Application.UnitTests.TestDoubles;

internal sealed class FakeCurrentUser : ICurrentUser
{
    public SenderId SenderId { get; } = SenderId.Create(Guid.Parse("22222222-2222-2222-2222-222222222222")).Value;
}
