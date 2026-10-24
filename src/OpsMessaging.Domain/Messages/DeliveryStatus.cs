namespace OpsMessaging.Domain.Messages;

/// <summary>Status of one message to one recipient via one channel.</summary>
public enum DeliveryStatus
{
    /// <summary>Created, the channel has not reported back yet.</summary>
    Pending = 0,

    /// <summary>The channel accepted the message.</summary>
    Sent = 1,

    /// <summary>The channel could not deliver the message.</summary>
    Failed = 2,
}
