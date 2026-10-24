using OpsMessaging.Application.Abstractions;
using OpsMessaging.Domain.Messages;

namespace OpsMessaging.Application.UnitTests.TestDoubles;

/// <summary>Returns a fixed list of recipients and remembers which audience was asked for.</summary>
internal sealed class FakeRecipientDirectory : IRecipientDirectory
{
    public List<RecipientId> Recipients { get; } = [];

    public Audience? LastAudience { get; private set; }

    public bool WasCalled => LastAudience is not null;

    public FakeRecipientDirectory WithRecipients(params string[] ids)
    {
        Recipients.AddRange(ids.Select(id => RecipientId.Create(id).Value));
        return this;
    }

    public Task<IReadOnlyList<RecipientId>> FindRecipientsAsync(Audience audience, CancellationToken cancellationToken)
    {
        LastAudience = audience;
        return Task.FromResult<IReadOnlyList<RecipientId>>(Recipients);
    }
}
