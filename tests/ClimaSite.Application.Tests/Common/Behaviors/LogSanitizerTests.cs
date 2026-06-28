using ClimaSite.Application.Common.Behaviors;
using FluentAssertions;
using Xunit;

namespace ClimaSite.Application.Tests.Common.Behaviors;

public class LogSanitizerTests
{
    private sealed record FakeRegister(string Email, string Password, string FirstName);
    private sealed record FakeGoogle(string IdToken, bool EmailVerified);
    private sealed record FakeCard(string CardNumber, string Cvc, string Cvv, string Pin, decimal Amount);
    private sealed record CasingSample(string Password, string apitoken, string ClientSecret, string ConfirmPassword);
    private sealed record NonSensitive(string Slug, int Page);

    private sealed class WithThrowingGetter
    {
        public string Ok => "ok";
        public string Boom => throw new InvalidOperationException("getter blew up");
    }

    [Fact]
    public void Redact_Password_IsReplaced_OtherFieldsPreserved()
    {
        var result = LogSanitizer.Redact(new FakeRegister("a@b.com", "S3cret!Pass", "Ann"));

        result["Password"].Should().Be(LogSanitizer.Redacted);
        result["Email"].Should().Be("a@b.com");      // non-sensitive value still visible for debugging
        result["FirstName"].Should().Be("Ann");
    }

    [Fact]
    public void Redact_IdToken_IsRedacted()
    {
        var result = LogSanitizer.Redact(new FakeGoogle("eyJhbGciOiJSUzI1NiIsImtpZCI6...", true));

        result["IdToken"].Should().Be(LogSanitizer.Redacted);
        result["EmailVerified"].Should().Be(true);
    }

    [Fact]
    public void Redact_CardFields_AreRedacted()
    {
        var result = LogSanitizer.Redact(new FakeCard("4242424242424242", "123", "456", "0000", 9.99m));

        result["CardNumber"].Should().Be(LogSanitizer.Redacted);
        result["Cvc"].Should().Be(LogSanitizer.Redacted);
        result["Cvv"].Should().Be(LogSanitizer.Redacted);
        result["Pin"].Should().Be(LogSanitizer.Redacted);
        result["Amount"].Should().Be(9.99m);
    }

    [Fact]
    public void Redact_IsCaseInsensitive_AndMatchesSubstrings()
    {
        // Password (case vs lowercase marker), apitoken (token), ClientSecret (secret), ConfirmPassword (substring).
        var result = LogSanitizer.Redact(new CasingSample("a", "b", "c", "d"));

        result.Values.Should().AllBeEquivalentTo(LogSanitizer.Redacted);
    }

    [Fact]
    public void Redact_NonSensitiveCommand_PassesAllValuesThrough()
    {
        var result = LogSanitizer.Redact(new NonSensitive("acme-ac", 2));

        result["Slug"].Should().Be("acme-ac");
        result["Page"].Should().Be(2);
        result.Values.Should().NotContain(LogSanitizer.Redacted);
    }

    [Fact]
    public void Redact_ThrowingGetter_DoesNotThrow_AndYieldsNull()
    {
        IReadOnlyDictionary<string, object?> result = null!;
        var act = () => result = LogSanitizer.Redact(new WithThrowingGetter());

        act.Should().NotThrow();
        result["Ok"].Should().Be("ok");
        result["Boom"].Should().BeNull();
    }

    [Fact]
    public void Redact_Null_Throws()
    {
        var act = () => LogSanitizer.Redact(null!);

        act.Should().Throw<ArgumentNullException>();
    }

    private sealed record Inner(string Email, string AccessToken);
    private sealed record Outer(string Name, Inner Details, IReadOnlyList<Inner> Items);

    [Fact]
    public void Redact_RecursesIntoNestedObjectsAndCollections()
    {
        // A secret nested under a NON-sensitive-named field ("Details"/"Items") must still be redacted,
        // because Serilog destructures nested objects too.
        var request = new Outer(
            "Ann",
            new Inner("a@b.com", "tok-secret-1"),
            new[] { new Inner("c@d.com", "tok-secret-2") });

        var result = LogSanitizer.Redact(request);

        result["Name"].Should().Be("Ann");
        var nested = result["Details"].Should().BeAssignableTo<IReadOnlyDictionary<string, object?>>().Subject;
        nested["Email"].Should().Be("a@b.com");
        nested["AccessToken"].Should().Be(LogSanitizer.Redacted);

        var items = result["Items"].Should().BeAssignableTo<System.Collections.IEnumerable>().Subject
            .Cast<IReadOnlyDictionary<string, object?>>().ToList();
        items.Should().ContainSingle();
        items[0]["AccessToken"].Should().Be(LogSanitizer.Redacted);
        items[0]["Email"].Should().Be("c@d.com");
    }
}
