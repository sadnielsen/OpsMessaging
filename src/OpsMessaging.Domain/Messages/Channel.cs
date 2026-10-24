namespace OpsMessaging.Domain.Messages;

/// <summary>A route through which a message reaches an employee.</summary>
public enum Channel
{
    /// <summary>Push notification to the work app of the employee.</summary>
    Push = 0,

    /// <summary>Text message to the work phone of the employee.</summary>
    Sms = 1,

    /// <summary>E-mail to the work mailbox of the employee.</summary>
    Email = 2,
}
