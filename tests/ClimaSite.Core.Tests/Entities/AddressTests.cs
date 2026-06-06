using ClimaSite.Core.Entities;
using FluentAssertions;

namespace ClimaSite.Core.Tests.Entities;

public class AddressTests
{
    private static Address CreateValidAddress() =>
        new(
            Guid.NewGuid(),
            "Test User",
            "123 Test Street",
            "Sofia",
            "1000",
            "Bulgaria",
            "BG");

    [Fact]
    public void Constructor_WithValidData_CreatesShippingAddress()
    {
        var address = CreateValidAddress();

        address.FullName.Should().Be("Test User");
        address.AddressLine1.Should().Be("123 Test Street");
        address.City.Should().Be("Sofia");
        address.PostalCode.Should().Be("1000");
        address.Country.Should().Be("Bulgaria");
        address.CountryCode.Should().Be("BG");
        address.Type.Should().Be(AddressType.Shipping);
    }

    [Fact]
    public void SetType_WithBoth_UpdatesType()
    {
        var address = CreateValidAddress();

        address.SetType(AddressType.Both);

        address.Type.Should().Be(AddressType.Both);
    }
}
