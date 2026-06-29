using ClimaSite.Application.Common.Pricing;
using FluentAssertions;

namespace ClimaSite.Api.Tests.Infrastructure;

/// <summary>
/// Direct guards on <see cref="FakePaymentService"/>'s idempotency modelling. The integration
/// tests lean on this double faithfully mirroring Stripe (same key + same params ⇒ replay; same
/// key + ANY differing param ⇒ 400), so the double's own logic is worth pinning — in particular
/// the metadata fingerprint, which the HTTP-level tests can only exercise alongside an amount
/// change (no two allowed shipping methods share a cost), so it is guarded independently here.
/// </summary>
public class FakePaymentServiceTests
{
    private const string Key = "ci_11111111-1111-1111-1111-111111111111";

    private static Dictionary<string, string> Meta(string cartId) => new()
    {
        ["cartId"] = cartId,
        ["shippingMethod"] = "standard"
    };

    [Fact]
    public async Task SameKey_SameParams_ReplaysTheSameIntent()
    {
        var sut = new FakePaymentService();

        var first = await sut.CreatePaymentIntentAsync(100m, "eur", Meta("cart-A"), Key);
        var second = await sut.CreatePaymentIntentAsync(100m, "eur", Meta("cart-A"), Key);

        first.Succeeded.Should().BeTrue();
        second.Succeeded.Should().BeTrue();
        second.PaymentIntentId.Should().Be(first.PaymentIntentId, "an unchanged request replays the cached intent");
        sut.CreatedIntents.Should().HaveCount(1, "the replay must not mint a second intent");
    }

    [Fact]
    public async Task SameKey_DifferentMetadataOnly_IsRejectedAsParamMismatch()
    {
        var sut = new FakePaymentService();

        // Identical amount + currency, ONLY the metadata differs — real Stripe hashes the whole
        // request body, so this must be rejected exactly like an amount mismatch.
        var first = await sut.CreatePaymentIntentAsync(100m, "eur", Meta("cart-A"), Key);
        var second = await sut.CreatePaymentIntentAsync(100m, "eur", Meta("cart-B"), Key);

        first.Succeeded.Should().BeTrue();
        second.Succeeded.Should().BeFalse();
        second.ErrorMessage.Should().Contain("idempotency key reused with different parameters");
        sut.CreatedIntents.Should().HaveCount(1, "the rejected call must not mint a second intent");
    }

    [Fact]
    public async Task SameKey_DifferentAmount_IsRejectedAsParamMismatch()
    {
        var sut = new FakePaymentService();

        var first = await sut.CreatePaymentIntentAsync(100m, "eur", Meta("cart-A"), Key);
        var second = await sut.CreatePaymentIntentAsync(250m, "eur", Meta("cart-A"), Key);

        first.Succeeded.Should().BeTrue();
        second.Succeeded.Should().BeFalse();
        second.ErrorMessage.Should().Contain("idempotency key reused with different parameters");
    }

    [Fact]
    public async Task NoKey_AlwaysMintsADistinctIntent()
    {
        var sut = new FakePaymentService();

        var first = await sut.CreatePaymentIntentAsync(100m, "eur", Meta("cart-A"));
        var second = await sut.CreatePaymentIntentAsync(100m, "eur", Meta("cart-A"));

        second.PaymentIntentId.Should().NotBe(first.PaymentIntentId, "the null-key path keeps today's no-dedup behaviour");
        sut.CreatedIntents.Should().HaveCount(2);
        sut.CreateIdempotencyKeys.Should().AllSatisfy(k => k.Should().BeNull());
    }
}
