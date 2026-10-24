using OpsMessaging.Domain.Common;

namespace OpsMessaging.Domain.Messages;

/// <summary>
/// An operational message from a dispatcher to employees in the field: the aggregate root of this domain.
/// </summary>
/// <remarks>
/// CONCEPT (DDD): An aggregate root is the single entry point to a cluster of objects (here: the message
/// and its <see cref="Delivery"/> records). All changes go through its methods, which enforce the
/// business rules BR-01..BR-07. There are no public setters, so an invalid Message cannot exist.
/// The domain never reads the system clock: the current time is passed in (<c>now</c>), which keeps the
/// rules deterministic and easy to test.
/// </remarks>
public sealed class Message
{
    /// <summary>Minimum number of distinct channels for an urgent message (BR-03).</summary>
    public const int MinimumChannelsForUrgent = 2;

    /// <summary>How far ahead <see cref="ValidUntil"/> may lie (BR-04).</summary>
    public static readonly TimeSpan MaximumValidity = TimeSpan.FromHours(24);

    private readonly List<Delivery> _deliveries = [];
    private readonly List<IDomainEvent> _domainEvents = [];

    private Message(
        MessageTitle title,
        MessageBody body,
        Priority priority,
        IReadOnlyList<Channel> channels,
        Audience audience,
        DateTimeOffset validUntil,
        SenderId senderId,
        DateTimeOffset createdAt)
    {
        Id = Guid.NewGuid();
        Title = title;
        Body = body;
        Priority = priority;
        Channels = channels;
        Audience = audience;
        ValidUntil = validUntil;
        SenderId = senderId;
        CreatedAt = createdAt;
        Status = MessageStatus.Created;
    }

    /// <summary>Unique identifier of the message.</summary>
    public Guid Id { get; }

    /// <summary>Short headline.</summary>
    public MessageTitle Title { get; }

    /// <summary>The content.</summary>
    public MessageBody Body { get; }

    /// <summary>Normal or urgent.</summary>
    public Priority Priority { get; }

    /// <summary>The distinct channels the message is sent through.</summary>
    public IReadOnlyList<Channel> Channels { get; }

    /// <summary>Who the message is meant for (a description, not a list of people).</summary>
    public Audience Audience { get; }

    /// <summary>Until when the message is relevant. Afterwards it counts as expired.</summary>
    public DateTimeOffset ValidUntil { get; }

    /// <summary>Who sent the message.</summary>
    public SenderId SenderId { get; }

    /// <summary>When the message was created.</summary>
    public DateTimeOffset CreatedAt { get; }

    /// <summary>When the message was dispatched; <c>null</c> before dispatch.</summary>
    public DateTimeOffset? DispatchedAt { get; private set; }

    /// <summary>When the message was cancelled; <c>null</c> if it was not cancelled.</summary>
    public DateTimeOffset? CancelledAt { get; private set; }

    /// <summary>The stored status. Use <see cref="GetEffectiveStatus"/> to also see "Expired".</summary>
    public MessageStatus Status { get; private set; }

    /// <summary>One delivery per recipient per channel, created by <see cref="Dispatch"/>.</summary>
    public IReadOnlyList<Delivery> Deliveries => _deliveries;

    /// <summary>Events raised by this aggregate that have not been published yet.</summary>
    public IReadOnlyList<IDomainEvent> DomainEvents => _domainEvents;

    /// <summary>
    /// Creates a new message after checking the rules that span several fields (BR-03, BR-04).
    /// Title, body and audience are value objects, so BR-01 and BR-02 were already checked when they were created.
    /// </summary>
    /// <param name="title">Validated title (BR-01).</param>
    /// <param name="body">Validated body (BR-01).</param>
    /// <param name="priority">Normal or urgent.</param>
    /// <param name="channels">Requested channels; duplicates are ignored.</param>
    /// <param name="audience">Validated audience (BR-02).</param>
    /// <param name="validUntil">Until when the message is relevant.</param>
    /// <param name="senderId">Who sends the message.</param>
    /// <param name="now">The current time, from the clock of the caller.</param>
    public static Result<Message> Create(
        MessageTitle title,
        MessageBody body,
        Priority priority,
        IEnumerable<Channel> channels,
        Audience audience,
        DateTimeOffset validUntil,
        SenderId senderId,
        DateTimeOffset now)
    {
        if (!Enum.IsDefined(priority))
        {
            return Result.Failure<Message>(Error.Validation("priority", "Unknown priority."));
        }

        // Duplicates do not add redundancy: "Push, Push" is still one channel.
        var distinctChannels = channels.Distinct().ToList();

        if (distinctChannels.Any(channel => !Enum.IsDefined(channel)))
        {
            return Result.Failure<Message>(Error.Validation("channels", "Unknown channel."));
        }

        // BR-03: every message needs a channel; an urgent message needs at least two (redundancy).
        if (distinctChannels.Count == 0)
        {
            return Result.Failure<Message>(MessageErrors.NoChannels);
        }

        if (priority == Priority.Urgent && distinctChannels.Count < MinimumChannelsForUrgent)
        {
            return Result.Failure<Message>(MessageErrors.UrgentNeedsMoreChannels);
        }

        // BR-04: ValidUntil lies in the future and at most 24 hours after now.
        if (validUntil <= now)
        {
            return Result.Failure<Message>(MessageErrors.ValidUntilNotInFuture);
        }

        if (validUntil > now + MaximumValidity)
        {
            return Result.Failure<Message>(MessageErrors.ValidUntilTooFarAhead);
        }

        return Result.Success(
            new Message(title, body, priority, distinctChannels, audience, validUntil, senderId, now));
    }

    /// <summary>
    /// Dispatches the message to the resolved recipients: creates one pending <see cref="Delivery"/>
    /// per recipient per channel and raises <see cref="MessageDispatched"/>.
    /// </summary>
    /// <param name="recipients">The recipients the audience resolved to; duplicates are ignored.</param>
    /// <param name="now">The current time, from the clock of the caller.</param>
    public Result Dispatch(IEnumerable<RecipientId> recipients, DateTimeOffset now)
    {
        // Dispatching twice means the calling code is wrong; that is unexpected, hence an exception.
        if (Status != MessageStatus.Created)
        {
            throw new InvalidOperationException($"Message {Id} has status {Status} and cannot be dispatched again.");
        }

        var distinctRecipients = recipients.Distinct().ToList();

        // BR-05: if the audience resolves to zero recipients, the message is not sent.
        if (distinctRecipients.Count == 0)
        {
            return Result.Failure(MessageErrors.NoRecipients);
        }

        foreach (var recipient in distinctRecipients)
        {
            foreach (var channel in Channels)
            {
                _deliveries.Add(new Delivery(recipient, channel));
            }
        }

        Status = MessageStatus.Dispatched;
        DispatchedAt = now;

        _domainEvents.Add(new MessageDispatched(
            Id, SenderId, Priority, Channels, distinctRecipients.Count, _deliveries.Count, now));

        return Result.Success();
    }

    /// <summary>Cancels (withdraws) a dispatched message that has not expired yet.</summary>
    /// <param name="now">The current time, from the clock of the caller.</param>
    public Result Cancel(DateTimeOffset now)
    {
        // BR-06: only a message with status Dispatched can be cancelled.
        if (Status != MessageStatus.Dispatched)
        {
            return Result.Failure(MessageErrors.OnlyDispatchedCanBeCancelled);
        }

        // BR-07: an expired message (ValidUntil < now) can no longer be cancelled.
        if (IsExpired(now))
        {
            return Result.Failure(MessageErrors.ExpiredCannotBeCancelled);
        }

        Status = MessageStatus.Cancelled;
        CancelledAt = now;
        return Result.Success();
    }

    /// <summary>Records that the channel delivered the message to the recipient.</summary>
    public void MarkDeliverySent(RecipientId recipientId, Channel channel) =>
        FindDelivery(recipientId, channel).MarkSent();

    /// <summary>Records that the channel could not deliver the message to the recipient.</summary>
    public void MarkDeliveryFailed(RecipientId recipientId, Channel channel) =>
        FindDelivery(recipientId, channel).MarkFailed();

    /// <summary>
    /// The status as a reader should see it: a dispatched message whose <see cref="ValidUntil"/> has
    /// passed is reported as <see cref="MessageStatus.Expired"/>. "Expired" is derived, never stored.
    /// </summary>
    /// <param name="now">The current time, from the clock of the caller.</param>
    public MessageStatus GetEffectiveStatus(DateTimeOffset now) =>
        Status == MessageStatus.Dispatched && IsExpired(now) ? MessageStatus.Expired : Status;

    /// <summary>Removes the raised events, after the Application layer has published them.</summary>
    public void ClearDomainEvents() => _domainEvents.Clear();

    private bool IsExpired(DateTimeOffset now) => ValidUntil < now;

    private Delivery FindDelivery(RecipientId recipientId, Channel channel) =>
        _deliveries.SingleOrDefault(d => d.RecipientId == recipientId && d.Channel == channel)
        ?? throw new InvalidOperationException($"Message {Id} has no delivery to {recipientId} via {channel}.");
}
