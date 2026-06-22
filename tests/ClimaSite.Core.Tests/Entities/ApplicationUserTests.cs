using ClimaSite.Core.Entities;
using FluentAssertions;

namespace ClimaSite.Core.Tests.Entities;

public class ApplicationUserTests
{
    [Fact]
    public void FullName_CombinesFirstAndLastName()
    {
        var user = new ApplicationUser { FirstName = "Ada", LastName = "Lovelace" };

        user.FullName.Should().Be("Ada Lovelace");
    }

    [Fact]
    public void FullName_WithOnlyFirstName_TrimsTrailingSpace()
    {
        var user = new ApplicationUser { FirstName = "Ada", LastName = "" };

        user.FullName.Should().Be("Ada");
    }

    [Fact]
    public void FullName_WithNoNames_IsEmpty()
    {
        var user = new ApplicationUser();

        user.FullName.Should().BeEmpty();
    }

    [Fact]
    public void RecordLogin_SetsLastLoginAt()
    {
        var user = new ApplicationUser();

        user.RecordLogin();

        user.LastLoginAt.Should().NotBeNull();
        user.LastLoginAt.Should().BeOnOrBefore(DateTime.UtcNow);
    }

    [Fact]
    public void SetRefreshToken_SetsTokenAndExpiry()
    {
        var user = new ApplicationUser();
        var expiry = DateTime.UtcNow.AddDays(7);

        user.SetRefreshToken("token-abc", expiry);

        user.RefreshToken.Should().Be("token-abc");
        user.RefreshTokenExpiryTime.Should().Be(expiry);
    }

    [Fact]
    public void RevokeRefreshToken_ClearsTokenAndExpiry()
    {
        var user = new ApplicationUser();
        user.SetRefreshToken("token-abc", DateTime.UtcNow.AddDays(7));

        user.RevokeRefreshToken();

        user.RefreshToken.Should().BeNull();
        user.RefreshTokenExpiryTime.Should().BeNull();
    }

    [Fact]
    public void IsRefreshTokenValid_WithValidNonExpiredToken_ReturnsTrue()
    {
        var user = new ApplicationUser();
        user.SetRefreshToken("token-abc", DateTime.UtcNow.AddMinutes(10));

        user.IsRefreshTokenValid().Should().BeTrue();
    }

    [Fact]
    public void IsRefreshTokenValid_WithExpiredToken_ReturnsFalse()
    {
        var user = new ApplicationUser();
        user.SetRefreshToken("token-abc", DateTime.UtcNow.AddMinutes(-10));

        user.IsRefreshTokenValid().Should().BeFalse();
    }

    [Fact]
    public void IsRefreshTokenValid_WithNoToken_ReturnsFalse()
    {
        var user = new ApplicationUser();

        user.IsRefreshTokenValid().Should().BeFalse();
    }

    [Fact]
    public void IsRefreshTokenValid_AfterRevoke_ReturnsFalse()
    {
        var user = new ApplicationUser();
        user.SetRefreshToken("token-abc", DateTime.UtcNow.AddDays(1));
        user.RevokeRefreshToken();

        user.IsRefreshTokenValid().Should().BeFalse();
    }

    [Fact]
    public void DefaultValues_AreSet()
    {
        var user = new ApplicationUser();

        user.IsActive.Should().BeTrue();
        user.PreferredLanguage.Should().Be("en");
        user.PreferredCurrency.Should().Be("USD");
    }
}
