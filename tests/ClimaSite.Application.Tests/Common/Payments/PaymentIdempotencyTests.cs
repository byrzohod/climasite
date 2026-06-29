using ClimaSite.Application.Common.Payments;
using FluentAssertions;

namespace ClimaSite.Application.Tests.Common.Payments;

public class PaymentIdempotencyTests
{
    [Fact]
    public void ForRefund_IsDeterministic_ForTheSameIntentId()
    {
        const string intentId = "pi_test_abc123";

        var first = PaymentIdempotency.ForRefund(intentId);
        var second = PaymentIdempotency.ForRefund(intentId);

        first.Should().Be(second);
    }

    [Fact]
    public void ForRefund_DiffersPerIntentId()
    {
        var a = PaymentIdempotency.ForRefund("pi_aaa");
        var b = PaymentIdempotency.ForRefund("pi_bbb");

        a.Should().NotBe(b);
    }

    [Fact]
    public void ForRefund_HasReV1PrefixAnd64HexBody()
    {
        var key = PaymentIdempotency.ForRefund("pi_shape_check");

        key.Should().StartWith("re_v1_");

        var body = key["re_v1_".Length..];
        body.Should().HaveLength(64);
        body.Should().MatchRegex("^[0-9a-f]{64}$");
    }

    [Fact]
    public void ForRefund_DoesNotEmbedTheRawIntentId()
    {
        // The intent id must not be echoed into the key (hashed, no PII forwarded to Stripe).
        const string intentId = "pi_secret_intent_id";

        var key = PaymentIdempotency.ForRefund(intentId);

        key.Should().NotContain(intentId);
    }

    [Fact]
    public void IsValidClientKey_AcceptsAGuid()
    {
        PaymentIdempotency.IsValidClientKey(Guid.NewGuid().ToString()).Should().BeTrue();
        PaymentIdempotency.IsValidClientKey(Guid.NewGuid().ToString("N")).Should().BeTrue();
    }

    [Fact]
    public void IsValidClientKey_RejectsTooShort()
    {
        PaymentIdempotency.IsValidClientKey(new string('a', 7)).Should().BeFalse();
    }

    [Fact]
    public void IsValidClientKey_RejectsTooLong()
    {
        PaymentIdempotency.IsValidClientKey(new string('a', 201)).Should().BeFalse();
    }

    [Fact]
    public void IsValidClientKey_RejectsIllegalCharacters()
    {
        PaymentIdempotency.IsValidClientKey("bad key!").Should().BeFalse();
    }

    [Fact]
    public void IsValidClientKey_AcceptsBoundaryLengths()
    {
        PaymentIdempotency.IsValidClientKey(new string('a', 8)).Should().BeTrue();
        PaymentIdempotency.IsValidClientKey(new string('a', 200)).Should().BeTrue();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void NormalizeClientKey_NullOrEmpty_ReturnsNull(string? raw)
    {
        PaymentIdempotency.NormalizeClientKey(raw).Should().BeNull();
    }

    [Fact]
    public void NormalizeClientKey_NonEmpty_PrefixesWithCi()
    {
        PaymentIdempotency.NormalizeClientKey("abc").Should().Be("ci_abc");
    }
}
