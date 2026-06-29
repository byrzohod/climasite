using System.Reflection;
using ClimaSite.Api.Controllers;
using FluentAssertions;
using Microsoft.AspNetCore.RateLimiting;

namespace ClimaSite.Api.Tests.Features.Installation;

/// <summary>
/// B-034: the anonymous installation lead endpoint writes PII + enqueues an outbox email, so it must carry
/// the same strict per-IP budget as the contact form. The actual 429 is exercised in /acceptance against a
/// Development run (rate limiting is intentionally disabled in the Testing integration env), so this guards
/// the wiring deterministically: the policy attribute must be present and name the "strict" policy.
/// </summary>
public class InstallationRateLimitTests
{
    [Fact]
    public void CreateInstallationRequest_IsGuardedByTheStrictRateLimitPolicy()
    {
        var method = typeof(InstallationController)
            .GetMethod(nameof(InstallationController.CreateInstallationRequest));

        method.Should().NotBeNull();

        var attribute = method!.GetCustomAttribute<EnableRateLimitingAttribute>();
        attribute.Should().NotBeNull("the anonymous PII lead endpoint must be rate-limited (B-034)");
        attribute!.PolicyName.Should().Be("strict");
    }
}
