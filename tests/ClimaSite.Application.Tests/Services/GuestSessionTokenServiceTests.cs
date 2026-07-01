using ClimaSite.Infrastructure.Services;
using FluentAssertions;

namespace ClimaSite.Application.Tests.Services;

/// <summary>
/// INV-01 Wave A0: the signed guest-session token (<c>id.expUnixSeconds.signature</c>) is the security
/// foundation that replaces the spoofable client-supplied guest id. These cover the round-trip, the
/// cryptographically-bound expiry, and every tamper vector (id, exp, signature, foreign key, malformed shapes)
/// plus the uniqueness guarantee.
/// </summary>
public class GuestSessionTokenServiceTests
{
    // Two independent >=32-char secrets so the "signed with a different key" case is a genuinely foreign key.
    private const string SecretA = "guest-session-unit-test-secret-A-0123456789";
    private const string SecretB = "guest-session-unit-test-secret-B-9876543210";

    private static GuestSessionTokenService Service(string secret = SecretA) => new(secret);

    [Fact]
    public void Issue_ThenTryValidate_RoundTrips()
    {
        var service = Service();

        var token = service.Issue();
        var valid = service.TryValidate(token, out var id);

        valid.Should().BeTrue();
        id.Should().NotBeNullOrEmpty();
        token.Should().StartWith(id + ".");
        token.Split('.').Should().HaveCount(3, "the token is id.expUnixSeconds.signature");
    }

    [Fact]
    public void TwoIssueCalls_ProduceDifferentIds()
    {
        var service = Service();

        service.TryValidate(service.Issue(), out var first).Should().BeTrue();
        service.TryValidate(service.Issue(), out var second).Should().BeTrue();

        first.Should().NotBe(second);
    }

    [Fact]
    public void TryValidate_RejectsTamperedId()
    {
        var service = Service();
        var parts = service.Issue().Split('.');

        // Flip the first id character to another valid base64url character, keeping exp + signature.
        var tamperedFirst = parts[0][0] == 'A' ? 'B' : 'A';
        var tamperedToken = $"{tamperedFirst}{parts[0][1..]}.{parts[1]}.{parts[2]}";

        service.TryValidate(tamperedToken, out var outId).Should().BeFalse();
        outId.Should().BeEmpty();
    }

    [Fact]
    public void TryValidate_RejectsTamperedExpiry()
    {
        var service = Service();
        var parts = service.Issue().Split('.');

        // Push the expiry far into the future while keeping the original signature — must fail because the
        // signature covers "{id}.{exp}", so a client cannot extend its own lease.
        var extendedExp = (long.Parse(parts[1]) + 86_400).ToString();
        var tamperedToken = $"{parts[0]}.{extendedExp}.{parts[2]}";

        service.TryValidate(tamperedToken, out _).Should().BeFalse();
    }

    [Fact]
    public void TryValidate_RejectsTamperedSignature()
    {
        var service = Service();
        var parts = service.Issue().Split('.');

        var tamperedLast = parts[2][^1] == 'A' ? 'B' : 'A';
        var tamperedToken = $"{parts[0]}.{parts[1]}.{parts[2][..^1]}{tamperedLast}";

        service.TryValidate(tamperedToken, out _).Should().BeFalse();
    }

    [Fact]
    public void TryValidate_RejectsTokenSignedWithADifferentKey()
    {
        var issuer = Service(SecretB);
        var verifier = Service(SecretA);

        var foreignToken = issuer.Issue();

        verifier.TryValidate(foreignToken, out _).Should().BeFalse();
    }

    [Fact]
    public void TryValidate_RejectsAnExpiredButValidlySignedToken()
    {
        var service = Service();

        // Authentic signature, but the bound expiry is in the past.
        var expired = service.Issue(DateTimeOffset.UtcNow.AddSeconds(-1));

        service.TryValidate(expired, out var id).Should().BeFalse();
        id.Should().BeEmpty();
    }

    [Fact]
    public void TryValidate_AcceptsAFutureExpiryMintedViaTheOverload()
    {
        var service = Service();

        var token = service.Issue(DateTimeOffset.UtcNow.AddMinutes(5));

        service.TryValidate(token, out var id).Should().BeTrue();
        id.Should().NotBeNullOrEmpty();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("no-separator-present")]
    [InlineData("id.9999999999")]           // only two parts
    [InlineData("id.9999999999.sig.extra")] // four parts
    [InlineData("..")]                        // empty parts
    [InlineData("id..sig")]                   // empty exp
    [InlineData("id.notanumber.sig")]         // non-numeric exp
    [InlineData("id.9999999999.$$$")]         // signature not base64url
    public void TryValidate_RejectsMalformedTokens(string? token)
    {
        var service = Service();

        service.TryValidate(token, out var id).Should().BeFalse();
        id.Should().BeEmpty();
    }

    [Fact]
    public void Constructor_RejectsAnEmptySecret()
    {
        var act = () => new GuestSessionTokenService(string.Empty);

        act.Should().Throw<ArgumentException>();
    }
}
