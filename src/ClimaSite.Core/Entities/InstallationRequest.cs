namespace ClimaSite.Core.Entities;

public class InstallationRequest : BaseEntity
{
    public Guid? UserId { get; private set; }
    public Guid? OrderId { get; private set; }
    public Guid ProductId { get; private set; }
    public string ProductName { get; private set; } = string.Empty;
    public InstallationType InstallationType { get; private set; }
    public InstallationRequestStatus Status { get; private set; } = InstallationRequestStatus.Pending;

    // Customer information
    public string CustomerName { get; private set; } = string.Empty;
    public string CustomerEmail { get; private set; } = string.Empty;
    public string CustomerPhone { get; private set; } = string.Empty;

    // Installation address
    public string AddressLine1 { get; private set; } = string.Empty;
    public string? AddressLine2 { get; private set; }
    public string City { get; private set; } = string.Empty;
    public string PostalCode { get; private set; } = string.Empty;
    public string Country { get; private set; } = string.Empty;

    // Scheduling
    public DateTime? PreferredDate { get; private set; }
    public string? PreferredTimeSlot { get; private set; }
    public DateTime? ScheduledDate { get; private set; }
    public DateTime? CompletedAt { get; private set; }

    // Additional details
    public string? Notes { get; private set; }
    public string? TechnicianNotes { get; private set; }
    public decimal EstimatedPrice { get; private set; }
    public decimal? FinalPrice { get; private set; }

    // Navigation properties
    public virtual ApplicationUser? User { get; private set; }
    public virtual Order? Order { get; private set; }
    public virtual Product Product { get; private set; } = null!;

    private InstallationRequest() { }

    public InstallationRequest(
        Guid productId,
        string productName,
        InstallationType installationType,
        string customerName,
        string customerEmail,
        string customerPhone,
        string addressLine1,
        string city,
        string postalCode,
        string country,
        decimal estimatedPrice)
    {
        if (string.IsNullOrWhiteSpace(customerName))
            throw new ArgumentException("Customer name is required", nameof(customerName));
        if (string.IsNullOrWhiteSpace(customerEmail))
            throw new ArgumentException("Customer email is required", nameof(customerEmail));
        if (string.IsNullOrWhiteSpace(customerPhone))
            throw new ArgumentException("Customer phone is required", nameof(customerPhone));
        if (string.IsNullOrWhiteSpace(addressLine1))
            throw new ArgumentException("Address is required", nameof(addressLine1));

        ProductId = productId;
        ProductName = productName;
        InstallationType = installationType;
        CustomerName = customerName;
        CustomerEmail = customerEmail;
        CustomerPhone = customerPhone;
        AddressLine1 = addressLine1;
        City = city;
        PostalCode = postalCode;
        Country = country;
        EstimatedPrice = estimatedPrice;
    }

    public void SetUser(Guid userId)
    {
        UserId = userId;
        SetUpdatedAt();
    }

    public void SetOrder(Guid orderId)
    {
        OrderId = orderId;
        SetUpdatedAt();
    }

    public void SetAddressLine2(string? addressLine2)
    {
        AddressLine2 = addressLine2?.Trim();
        SetUpdatedAt();
    }

    public void SetPreferredSchedule(DateTime? preferredDate, string? timeSlot)
    {
        PreferredDate = preferredDate;
        PreferredTimeSlot = timeSlot?.Trim();
        SetUpdatedAt();
    }

    public void SetNotes(string? notes)
    {
        Notes = notes?.Trim();
        SetUpdatedAt();
    }

    public void SetTechnicianNotes(string? notes)
    {
        TechnicianNotes = notes?.Trim();
        SetUpdatedAt();
    }

    public void Schedule(DateTime scheduledDate)
    {
        if (scheduledDate < DateTime.UtcNow)
            throw new ArgumentException("Scheduled date cannot be in the past", nameof(scheduledDate));

        ScheduledDate = scheduledDate;
        Status = InstallationRequestStatus.Scheduled;
        SetUpdatedAt();
    }

    public void Confirm()
    {
        Status = InstallationRequestStatus.Confirmed;
        SetUpdatedAt();
    }

    public void MarkInProgress()
    {
        Status = InstallationRequestStatus.InProgress;
        SetUpdatedAt();
    }

    public void Complete(decimal? finalPrice = null)
    {
        Status = InstallationRequestStatus.Completed;
        CompletedAt = DateTime.UtcNow;
        FinalPrice = finalPrice ?? EstimatedPrice;
        SetUpdatedAt();
    }

    public void Cancel()
    {
        Status = InstallationRequestStatus.Cancelled;
        SetUpdatedAt();
    }
}

public enum InstallationType
{
    Standard,
    Premium,
    Express
}

public enum InstallationRequestStatus
{
    Pending,
    Confirmed,
    Scheduled,
    InProgress,
    Completed,
    Cancelled
}
