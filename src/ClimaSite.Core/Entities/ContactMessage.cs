namespace ClimaSite.Core.Entities;

/// <summary>
/// A message submitted through the public contact form. Persisted so enquiries (sales leads,
/// complaints, GDPR requests) are never lost, and surfaced to the business by email via the outbox.
/// </summary>
public class ContactMessage : BaseEntity
{
    public string Name { get; private set; } = string.Empty;
    public string Email { get; private set; } = string.Empty;
    public string Subject { get; private set; } = string.Empty;
    public string Message { get; private set; } = string.Empty;
    public ContactMessageStatus Status { get; private set; } = ContactMessageStatus.New;

    private ContactMessage() { }

    public ContactMessage(string name, string email, string subject, string message)
    {
        Name = Require(name, nameof(name), 200);
        Email = Require(email, nameof(email), 320);
        Subject = Require(subject, nameof(subject), 200);
        Message = Require(message, nameof(message), 5000);
    }

    public void MarkRead()
    {
        Status = ContactMessageStatus.Read;
        SetUpdatedAt();
    }

    private static string Require(string value, string field, int maxLength)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException($"{field} is required", field);

        var trimmed = value.Trim();
        return trimmed.Length > maxLength ? trimmed[..maxLength] : trimmed;
    }
}

public enum ContactMessageStatus
{
    New = 0,
    Read = 1,
    Responded = 2,
    Archived = 3
}
