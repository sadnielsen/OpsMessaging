namespace OpsMessaging.Domain.Messages;

/// <summary>How urgent a message is. Determines the minimum number of channels (BR-03).</summary>
public enum Priority
{
    /// <summary>Regular operational information, for example a shift change.</summary>
    Normal = 0,

    /// <summary>Must reach people quickly, for example a disruption; requires redundant channels.</summary>
    Urgent = 1,
}
