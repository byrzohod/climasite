namespace ClimaSite.Application.Common.Options;

/// <summary>
/// Options for checkout-start stock reservations (INV-01 A2), bound from the "Reservations" configuration
/// section. Holds are minted at PaymentIntent creation and either consumed (order placed) or released by the
/// expiry sweeper. All numeric fields fall back to a safe built-in default when misconfigured (&lt;= 0) so a
/// bad config can never disable the anti-grief caps or mint an infinite-TTL hold.
/// </summary>
public class ReservationOptions
{
    public const string SectionName = "Reservations";

    /// <summary>How long a card hold lives before the sweeper may expire it. Default 20 minutes.</summary>
    public int HoldTtlMinutes { get; set; } = 20;

    /// <summary>
    /// Anti-grief cap on the number of units a single cart may hold of ONE variant. A reserve whose target for
    /// any variant exceeds this is rejected before any Stripe call. Bounds per-variant inventory denial by one
    /// guest/user (whose cart is 1:1 with their identity). Default 10.
    /// </summary>
    public int MaxUnitsPerVariant { get; set; } = 10;

    /// <summary>
    /// Anti-grief cap on the number of DISTINCT variant holds a single cart may take. Bounds the total holds one
    /// identity can pin at once. Default 20.
    /// </summary>
    public int MaxActiveLinesPerCart { get; set; } = 20;

    /// <summary>Expiry-sweeper settings (the sole releaser of expired holds).</summary>
    public SweeperOptions Sweeper { get; set; } = new();

    public int EffectiveHoldTtlMinutes => HoldTtlMinutes > 0 ? HoldTtlMinutes : 20;
    public int EffectiveMaxUnitsPerVariant => MaxUnitsPerVariant > 0 ? MaxUnitsPerVariant : 10;
    public int EffectiveMaxActiveLinesPerCart => MaxActiveLinesPerCart > 0 ? MaxActiveLinesPerCart : 20;

    public class SweeperOptions
    {
        /// <summary>
        /// Whether the background sweeper polls. Fail-closed in Production: the app fail-fasts at startup if this
        /// is false in Production (an unswept store leaks stock forever). Disabled in the Testing integration env
        /// (tests drive the sweep directly for determinism).
        /// </summary>
        public bool Enabled { get; set; } = true;

        /// <summary>Poll cadence in seconds. Default 30.</summary>
        public int PollIntervalSeconds { get; set; } = 30;

        /// <summary>Max expired holds reclaimed per tick. Default 100.</summary>
        public int BatchSize { get; set; } = 100;

        public int EffectivePollIntervalSeconds => PollIntervalSeconds > 0 ? PollIntervalSeconds : 30;
        public int EffectiveBatchSize => BatchSize > 0 ? BatchSize : 100;
    }
}
