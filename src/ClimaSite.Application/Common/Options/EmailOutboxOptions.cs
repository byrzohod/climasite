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
}
