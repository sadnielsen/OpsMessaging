namespace OpsMessaging.Domain.Messages;

/// <summary>Lifecycle status of a <see cref="Message"/> (see DESIGN.md §1.3).</summary>
public enum MessageStatus
{
    /// <summary>Created and validated, but not yet dispatched. Only exists inside the send use case.</summary>
    Created = 0,

    /// <summary>Sent to all recipients; can still be cancelled (BR-06).</summary>
    Dispatched = 1,

    /// <summary>Withdrawn by a supervisor. Final.</summary>
    Cancelled = 2,

    /// <summary>
    /// Dispatched, but <see cref="Message.ValidUntil"/> has passed. Never stored: it is derived from the
    /// current time by <see cref="Message.GetEffectiveStatus"/>.
    /// </summary>
    Expired = 3,
}
