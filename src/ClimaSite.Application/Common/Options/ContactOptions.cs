namespace ClimaSite.Application.Common.Options;

/// <summary>
/// Settings for the public contact form, bound from the "Contact" configuration section.
/// </summary>
public class ContactOptions
{
    public const string SectionName = "Contact";

    /// <summary>Business inbox that receives a notification email for each submitted enquiry.</summary>
    public string RecipientEmail { get; set; } = "support@climasite.local";
}
