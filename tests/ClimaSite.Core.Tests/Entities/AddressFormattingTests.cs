using ClimaSite.Core.Entities;
using FluentAssertions;

namespace ClimaSite.Core.Tests.Entities;

public class AddressFormattingTests
{
    private static Address CreateValid() =>
        new(Guid.NewGuid(), "Test User", "123 Test Street", "Sofia", "1000", "Bulgaria", "BG");

    [Fact]
    public void SetCountry_NormalizesCountryCodeToUpperInvariant()
    {
        var address = CreateValid();

        address.SetCountry("Germany", "de");

        address.Country.Should().Be("Germany");
        address.CountryCode.Should().Be("DE");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void SetCountry_WithEmptyCountry_ThrowsArgumentException(string country)
    {
        var address = CreateValid();

        var act = () => address.SetCountry(country, "BG");

        act.Should().Throw<ArgumentException>()
           .WithMessage("*Country cannot be empty*");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void SetCountry_WithEmptyCountryCode_ThrowsArgumentException(string code)
    {
        var address = CreateValid();

        var act = () => address.SetCountry("Bulgaria", code);

        act.Should().Throw<ArgumentException>()
           .WithMessage("*Country code cannot be empty*");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void SetFullName_WithEmptyValue_ThrowsArgumentException(string fullName)
    {
        var address = CreateValid();

        var act = () => address.SetFullName(fullName);

        act.Should().Throw<ArgumentException>()
           .WithMessage("*Full name cannot be empty*");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void SetAddressLine1_WithEmptyValue_ThrowsArgumentException(string line)
    {
        var address = CreateValid();

        var act = () => address.SetAddressLine1(line);

        act.Should().Throw<ArgumentException>()
           .WithMessage("*Address line 1 cannot be empty*");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void SetCity_WithEmptyValue_ThrowsArgumentException(string city)
    {
        var address = CreateValid();

        var act = () => address.SetCity(city);

        act.Should().Throw<ArgumentException>()
           .WithMessage("*City cannot be empty*");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void SetPostalCode_WithEmptyValue_ThrowsArgumentException(string postalCode)
    {
        var address = CreateValid();

        var act = () => address.SetPostalCode(postalCode);

        act.Should().Throw<ArgumentException>()
           .WithMessage("*Postal code cannot be empty*");
    }

    [Fact]
    public void SetAddressLine2_TrimsValue()
    {
        var address = CreateValid();

        address.SetAddressLine2("  Apt 7  ");

        address.AddressLine2.Should().Be("Apt 7");
    }

    [Fact]
    public void SetState_TrimsValue()
    {
        var address = CreateValid();

        address.SetState("  Sofia City  ");

        address.State.Should().Be("Sofia City");
    }

    [Fact]
    public void SetPhone_TrimsValue()
    {
        var address = CreateValid();

        address.SetPhone("  +359888123456  ");

        address.Phone.Should().Be("+359888123456");
    }

    [Fact]
    public void SetDefault_UpdatesValue()
    {
        var address = CreateValid();

        address.SetDefault(true);

        address.IsDefault.Should().BeTrue();
    }

    [Fact]
    public void ToFormattedString_WithoutOptionalParts_FormatsCoreFields()
    {
        var address = CreateValid();

        var formatted = address.ToFormattedString();

        formatted.Should().Contain("Test User");
        formatted.Should().Contain("123 Test Street");
        formatted.Should().Contain("Sofia,");
        formatted.Should().Contain("1000");
        formatted.Should().Contain("Bulgaria");
        formatted.Should().NotContain("\n\n");
    }

    [Fact]
    public void ToFormattedString_WithAddressLine2AndState_IncludesThem()
    {
        var address = CreateValid();
        address.SetAddressLine2("Floor 2");
        address.SetState("Sofia City");

        var formatted = address.ToFormattedString();

        formatted.Should().Contain("Floor 2");
        formatted.Should().Contain("Sofia City");
    }
}
