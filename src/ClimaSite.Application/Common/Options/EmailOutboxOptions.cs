namespace ClimaSite.Application.Common.Options;

/// <summary>
/// Tunables for the email outbox worker, bound from the "Outbox" configuration section.
/// </summary>
public class EmailOutboxOptions
{
    public const string SectionName = "Outbox";

    /// <summary>When false, the hosted worker loop does not run (e.g. integration tests drive it manually).</summary>
    public bool Enabled { get; set; } = true;

    /// <summary>Seconds the worker sleeps between drains of the outbox.</summary>
    public int PollIntervalSeconds { get; set; } = 15;

    /// <summary>Maximum number of messages claimed per drain.</summary>
    public int BatchSize { get; set; } = 25;

    /// <summary>Total delivery attempts before a message is marked permanently failed.</summary>
    public int MaxAttempts { get; set; } = 5;

    /// <summary>Base delay for exponential backoff between retries: delay = Base * 2^(attempt-1).</summary>
    public int BaseRetryDelaySeconds { get; set; } = 30;

    /// <summary>
    /// Minutes after which a row stuck in <c>Processing</c> (a worker crashed mid-attempt, between
    /// the Processing save and the terminal save) is reclaimed and retried. Without this, such a
    /// row would sit in Processing forever and the email would be silently lost.
    /// </summary>
    public int ProcessingReclaimMinutes { get; set; } = 10;
}
