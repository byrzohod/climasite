namespace ClimaSite.Application.Common.Options;

/// <summary>
/// Bank account details shown to a buyer who pays by bank transfer (GAP-06), bound from the
/// "BankTransfer" configuration section. The buyer wires the order total to this account and
/// quotes the order number as the payment reference; the order stays Pending until reconciled.
/// </summary>
public class BankTransferOptions
{
    public const string SectionName = "BankTransfer";

    /// <summary>IBAN the buyer wires the payment to.</summary>
    public string Iban { get; set; } = string.Empty;

    /// <summary>Account holder name shown to the buyer.</summary>
    public string AccountName { get; set; } = string.Empty;

    /// <summary>Bank name shown to the buyer.</summary>
    public string BankName { get; set; } = string.Empty;
}
