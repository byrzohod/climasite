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

    private sealed record FakeGuestCart(string GuestSessionId, int Quantity);
    private sealed record WithDictionary(IDictionary<string, string> Data);
    private sealed record WithList(IEnumerable<int> Items);
    private sealed record Throwing(IEnumerable<int> Items);
    private sealed record Deep1(Deep2 Next);
    private sealed record Deep2(Deep3 Next);
    private sealed record Deep3(Deep4 Next);
    private sealed record Deep4(Deep5 Next);
    private sealed record Deep5(string Password);

    private static IEnumerable<int> ThrowingSequence()
    {
        yield return 1;
        throw new InvalidOperationException("enumeration blew up");
    }

    [Fact]
    public void Redact_GuestSessionId_IsRedacted()
    {
        // A bearer-like key for anonymous cart/checkout — must not log in cleartext (council [High]).
        var result = LogSanitizer.Redact(new FakeGuestCart("sess-abc-123", 2));

        result["GuestSessionId"].Should().Be(LogSanitizer.Redacted);
        result["Quantity"].Should().Be(2);
    }

    [Fact]
    public void Redact_DictionaryWithSensitiveKey_RedactsValue()
    {
        var request = new WithDictionary(new Dictionary<string, string>
        {
            ["refreshToken"] = "leak-me",
            ["color"] = "blue"
        });

        var result = LogSanitizer.Redact(request);

        var data = result["Data"].Should().BeAssignableTo<IReadOnlyDictionary<string, object?>>().Subject;
        data["refreshToken"].Should().Be(LogSanitizer.Redacted);   // sensitive KEY → value redacted
        data["color"].Should().Be("blue");
    }

    [Fact]
    public void Redact_BeyondMaxDepth_DoesNotLeakViaToString()
    {
        // A secret nested deeper than MaxDepth must not slip out via record ToString().
        var request = new Deep1(new Deep2(new Deep3(new Deep4(new Deep5("DEEPSECRET")))));

        var flattened = System.Text.Json.JsonSerializer.Serialize(LogSanitizer.Redact(request));

        flattened.Should().NotContain("DEEPSECRET");
    }

    [Fact]
    public void Redact_LargeCollection_IsTruncated()
    {
        var result = LogSanitizer.Redact(new WithList(Enumerable.Range(1, 100)));

        var items = result["Items"].Should().BeAssignableTo<System.Collections.IEnumerable>()
            .Subject.Cast<object?>().ToList();
        items.Should().HaveCount(33);              // 32 items + a truncation marker
        items.Last().Should().Be("…(truncated)");
    }

    [Fact]
    public void Redact_ThrowingEnumerable_DoesNotThrow()
    {
        IReadOnlyDictionary<string, object?> result = null!;
        var act = () => result = LogSanitizer.Redact(new Throwing(ThrowingSequence()));

        act.Should().NotThrow();
        result["Items"].Should().Be("<unenumerable>");
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
