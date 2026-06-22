using System.Net;
using System.Net.Http.Json;
using ClimaSite.Api.Tests.Infrastructure;
using ClimaSite.Application.Features.Addresses;
using ClimaSite.Core.Entities;
using ClimaSite.Infrastructure.Data;
using FluentAssertions;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;

namespace ClimaSite.Api.Tests.Controllers;

/// <summary>
/// Integration coverage for the authenticated address CRUD surface
/// (<c>/api/addresses</c>): real HTTP -> MediatR handler -> Postgres.
/// Covers the happy path, 401 for anonymous callers, ownership isolation,
/// not-found, and validation rejection.
/// </summary>
public class AddressesControllerTests : IntegrationTestBase
{
    public AddressesControllerTests(TestWebApplicationFactory factory) : base(factory)
    {
    }

    private static object ValidAddressPayload(bool isDefault = false) => new
    {
        fullName = "Jane Customer",
        addressLine1 = "12 Cooling Way",
        addressLine2 = "Apt 4",
        city = "Sofia",
        state = "Sofia City",
        postalCode = "1000",
        country = "Bulgaria",
        countryCode = "BG",
        phone = "+359888123456",
        isDefault,
        type = "Shipping"
    };

    #region Auth

    [Fact]
    public async Task GetAddresses_Returns401_WhenUnauthenticated()
    {
        var response = await Client.GetAsync("/api/addresses");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task CreateAddress_Returns401_WhenUnauthenticated()
    {
        var response = await Client.PostAsJsonAsync("/api/addresses", ValidAddressPayload());

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task DeleteAddress_Returns401_WhenUnauthenticated()
    {
        var response = await Client.DeleteAsync($"/api/addresses/{Guid.NewGuid()}");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    #endregion

    #region Happy path

    [Fact]
    public async Task CreateAddress_PersistsAndReturnsCreated_ForAuthenticatedUser()
    {
        await AuthenticateAsync($"address-create-{Guid.NewGuid():N}@example.com");

        var response = await Client.PostAsJsonAsync("/api/addresses", ValidAddressPayload());

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var created = await response.Content.ReadFromJsonAsync<AddressDto>();
        created.Should().NotBeNull();
        created!.Id.Should().NotBeEmpty();
        created.FullName.Should().Be("Jane Customer");
        created.City.Should().Be("Sofia");
        created.CountryCode.Should().Be("BG");
        // First address auto-defaults per CreateAddressCommandHandler.
        created.IsDefault.Should().BeTrue();

        // The Location header should point at the by-id route.
        response.Headers.Location.Should().NotBeNull();
        response.Headers.Location!.ToString().Should().Contain(created.Id.ToString());
    }

    [Fact]
    public async Task GetAddresses_ReturnsOnlyTheCurrentUsersAddresses()
    {
        await AuthenticateAsync($"address-list-{Guid.NewGuid():N}@example.com");
        await Client.PostAsJsonAsync("/api/addresses", ValidAddressPayload());

        var response = await Client.GetAsync("/api/addresses");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var addresses = await response.Content.ReadFromJsonAsync<List<AddressDto>>();
        addresses.Should().NotBeNull();
        addresses!.Should().ContainSingle();
        addresses[0].FullName.Should().Be("Jane Customer");
    }

    [Fact]
    public async Task GetAddressById_ReturnsTheAddress_ForOwner()
    {
        await AuthenticateAsync($"address-byid-{Guid.NewGuid():N}@example.com");
        var createResponse = await Client.PostAsJsonAsync("/api/addresses", ValidAddressPayload());
        var created = await createResponse.Content.ReadFromJsonAsync<AddressDto>();

        var response = await Client.GetAsync($"/api/addresses/{created!.Id}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var fetched = await response.Content.ReadFromJsonAsync<AddressDto>();
        fetched!.Id.Should().Be(created.Id);
        fetched.AddressLine1.Should().Be("12 Cooling Way");
    }

    [Fact]
    public async Task UpdateAddress_ModifiesFields_ForOwner()
    {
        await AuthenticateAsync($"address-update-{Guid.NewGuid():N}@example.com");
        var createResponse = await Client.PostAsJsonAsync("/api/addresses", ValidAddressPayload());
        var created = await createResponse.Content.ReadFromJsonAsync<AddressDto>();

        var updatePayload = new
        {
            fullName = "Jane Updated",
            addressLine1 = "99 Heating Blvd",
            city = "Plovdiv",
            postalCode = "4000",
            country = "Bulgaria",
            countryCode = "BG",
            isDefault = true,
            type = "Billing"
        };

        var response = await Client.PutAsJsonAsync($"/api/addresses/{created!.Id}", updatePayload);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var updated = await response.Content.ReadFromJsonAsync<AddressDto>();
        updated!.FullName.Should().Be("Jane Updated");
        updated.City.Should().Be("Plovdiv");
        updated.AddressLine1.Should().Be("99 Heating Blvd");
        updated.Type.Should().Be("Billing");
    }

    [Fact]
    public async Task DeleteAddress_RemovesNonDefaultAddress_ForOwner()
    {
        await AuthenticateAsync($"address-delete-{Guid.NewGuid():N}@example.com");
        // First address becomes the default automatically; create a second, non-default one.
        await Client.PostAsJsonAsync("/api/addresses", ValidAddressPayload(isDefault: true));
        var secondResponse = await Client.PostAsJsonAsync("/api/addresses", ValidAddressPayload(isDefault: false));
        var second = await secondResponse.Content.ReadFromJsonAsync<AddressDto>();
        second!.IsDefault.Should().BeFalse();

        var response = await Client.DeleteAsync($"/api/addresses/{second.Id}");

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var listResponse = await Client.GetAsync("/api/addresses");
        var addresses = await listResponse.Content.ReadFromJsonAsync<List<AddressDto>>();
        addresses!.Should().ContainSingle();
        addresses!.Should().NotContain(a => a.Id == second.Id);
    }

    [Fact]
    public async Task SetDefaultAddress_PromotesTheChosenAddress_ForOwner()
    {
        await AuthenticateAsync($"address-default-{Guid.NewGuid():N}@example.com");
        var firstResponse = await Client.PostAsJsonAsync("/api/addresses", ValidAddressPayload(isDefault: true));
        var first = await firstResponse.Content.ReadFromJsonAsync<AddressDto>();
        var secondResponse = await Client.PostAsJsonAsync("/api/addresses", ValidAddressPayload(isDefault: false));
        var second = await secondResponse.Content.ReadFromJsonAsync<AddressDto>();

        var response = await Client.PutAsync($"/api/addresses/{second!.Id}/default", null);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var promoted = await response.Content.ReadFromJsonAsync<AddressDto>();
        promoted!.IsDefault.Should().BeTrue();

        // The previously-default address must no longer be default.
        var firstReread = await Client.GetAsync($"/api/addresses/{first!.Id}");
        var refreshedFirst = await firstReread.Content.ReadFromJsonAsync<AddressDto>();
        refreshedFirst!.IsDefault.Should().BeFalse();
    }

    #endregion

    #region Ownership + not found

    [Fact]
    public async Task GetAddressById_Returns404_WhenAddressBelongsToAnotherUser()
    {
        var ownerEmail = $"address-owner-{Guid.NewGuid():N}@example.com";
        await AuthenticateAsync(ownerEmail);
        var createResponse = await Client.PostAsJsonAsync("/api/addresses", ValidAddressPayload());
        var owned = await createResponse.Content.ReadFromJsonAsync<AddressDto>();

        // Switch to a different user.
        await AuthenticateAsync($"address-attacker-{Guid.NewGuid():N}@example.com");

        var response = await Client.GetAsync($"/api/addresses/{owned!.Id}");

        // The handler scopes by UserId, so a non-owner sees a NotFound (existence not revealed).
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task UpdateAddress_Returns400_WhenAddressBelongsToAnotherUser()
    {
        await AuthenticateAsync($"address-owner2-{Guid.NewGuid():N}@example.com");
        var createResponse = await Client.PostAsJsonAsync("/api/addresses", ValidAddressPayload());
        var owned = await createResponse.Content.ReadFromJsonAsync<AddressDto>();

        await AuthenticateAsync($"address-attacker2-{Guid.NewGuid():N}@example.com");

        var response = await Client.PutAsJsonAsync($"/api/addresses/{owned!.Id}", ValidAddressPayload());

        // UpdateAddressCommandHandler returns a failure Result ("Address not found") -> BadRequest.
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task GetAddressById_Returns404_ForUnknownId()
    {
        await AuthenticateAsync($"address-missing-{Guid.NewGuid():N}@example.com");

        var response = await Client.GetAsync($"/api/addresses/{Guid.NewGuid()}");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task DeleteAddress_Returns400_ForUnknownId()
    {
        await AuthenticateAsync($"address-delete-missing-{Guid.NewGuid():N}@example.com");

        var response = await Client.DeleteAsync($"/api/addresses/{Guid.NewGuid()}");

        // DeleteAddressCommandHandler returns a failure Result -> BadRequest, not 204.
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    #endregion

    #region Validation

    [Fact]
    public async Task CreateAddress_Returns400_WhenRequiredFieldsAreMissing()
    {
        await AuthenticateAsync($"address-invalid-{Guid.NewGuid():N}@example.com");

        var invalidPayload = new
        {
            fullName = "",          // invalid: required by the Address entity
            addressLine1 = "",      // invalid
            city = "",              // invalid
            postalCode = "",        // invalid
            country = "",           // invalid
            countryCode = "",       // invalid
            isDefault = false,
            type = "Shipping"
        };

        var response = await Client.PostAsJsonAsync("/api/addresses", invalidPayload);

        // The Address entity throws ArgumentException, surfaced as a failure Result -> BadRequest.
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    #endregion

    #region Direct-seed ownership guard

    /// <summary>
    /// Seeds an address straight into Postgres for one user, then confirms a second
    /// authenticated user cannot read it through the API. This exercises the
    /// controller -> handler -> DB ownership filter without relying on the create endpoint.
    /// </summary>
    [Fact]
    public async Task GetAddressById_Returns404_ForDirectlySeededForeignAddress()
    {
        var ownerEmail = $"address-seed-owner-{Guid.NewGuid():N}@example.com";
        await AuthenticateAsync(ownerEmail);
        var ownerId = await GetUserIdAsync(ownerEmail);

        Guid seededId;
        using (var scope = Factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var address = new Address(ownerId, "Seed Owner", "1 Seed St", "Varna", "9000", "Bulgaria", "BG");
            db.Addresses.Add(address);
            await db.SaveChangesAsync();
            seededId = address.Id;
        }

        await AuthenticateAsync($"address-seed-attacker-{Guid.NewGuid():N}@example.com");

        var response = await Client.GetAsync($"/api/addresses/{seededId}");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    #endregion

    private async Task<Guid> GetUserIdAsync(string email)
    {
        using var scope = Factory.Services.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var user = await userManager.FindByEmailAsync(email);
        user.Should().NotBeNull();
        return user!.Id;
    }
}
