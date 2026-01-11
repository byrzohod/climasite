namespace ClimaSite.Core.Entities;

public class Address : BaseEntity
{
    public Guid UserId { get; private set; }
    public string FullName { get; private set; } = string.Empty;
    public string AddressLine1 { get; private set; } = string.Empty;
    public string? AddressLine2 { get; private set; }
    public string City { get; private set; } = string.Empty;
    public string? State { get; private set; }
    public string PostalCode { get; private set; } = string.Empty;
    public string Country { get; private set; } = string.Empty;
    public string CountryCode { get; private set; } = string.Empty;
    public string? Phone { get; private set; }
    public bool IsDefault { get; private set; }
    public AddressType Type { get; private set; } = AddressType.Shipping;

    // Navigation properties
    public virtual ApplicationUser User { get; private set; } = null!;

    private Address() { }

    public Address(
        Guid userId,
        string fullName,
        string addressLine1,
        string city,
        string postalCode,
        string country,
        string countryCode)
    {
        UserId = userId;
        SetFullName(fullName);
        SetAddressLine1(addressLine1);
        SetCity(city);
        SetPostalCode(postalCode);
        SetCountry(country, countryCode);
    }

    public void SetFullName(string fullName)
    {
        if (string.IsNullOrWhiteSpace(fullName))
            throw new ArgumentException("Full name cannot be empty", nameof(fullName));

        FullName = fullName.Trim();
        SetUpdatedAt();
    }

    public void SetAddressLine1(string addressLine1)
    {
        if (string.IsNullOrWhiteSpace(addressLine1))
            throw new ArgumentException("Address line 1 cannot be empty", nameof(addressLine1));

        AddressLine1 = addressLine1.Trim();
        SetUpdatedAt();
    }

    public void SetAddressLine2(string? addressLine2)
    {
        AddressLine2 = addressLine2?.Trim();
        SetUpdatedAt();
    }

    public void SetCity(string city)
    {
        if (string.IsNullOrWhiteSpace(city))
            throw new ArgumentException("City cannot be empty", nameof(city));

        City = city.Trim();
        SetUpdatedAt();
    }

    public void SetState(string? state)
    {
        State = state?.Trim();
        SetUpdatedAt();
    }

    public void SetPostalCode(string postalCode)
    {
        if (string.IsNullOrWhiteSpace(postalCode))
            throw new ArgumentException("Postal code cannot be empty", nameof(postalCode));

        PostalCode = postalCode.Trim();
        SetUpdatedAt();
    }

    public void SetCountry(string country, string countryCode)
    {
        if (string.IsNullOrWhiteSpace(country))
            throw new ArgumentException("Country cannot be empty", nameof(country));

        if (string.IsNullOrWhiteSpace(countryCode))
            throw new ArgumentException("Country code cannot be empty", nameof(countryCode));

        Country = country.Trim();
        CountryCode = countryCode.Trim().ToUpperInvariant();
        SetUpdatedAt();
    }

    public void SetPhone(string? phone)
    {
        Phone = phone?.Trim();
        SetUpdatedAt();
    }

    public void SetDefault(bool isDefault)
    {
        IsDefault = isDefault;
        SetUpdatedAt();
    }

    public void SetType(AddressType type)
    {
        Type = type;
        SetUpdatedAt();
    }

    public string ToFormattedString()
    {
        var parts = new List<string> { FullName, AddressLine1 };
        if (!string.IsNullOrWhiteSpace(AddressLine2)) parts.Add(AddressLine2);
        parts.Add($"{City}, {State ?? ""} {PostalCode}".Trim());
        parts.Add(Country);
        return string.Join("\n", parts);
    }
}

public enum AddressType
{
    Shipping,
    Billing
}
