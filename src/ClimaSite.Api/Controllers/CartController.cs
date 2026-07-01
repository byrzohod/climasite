using ClimaSite.Api.Services;
using ClimaSite.Application.Features.Cart.Commands;
using ClimaSite.Application.Features.Cart.DTOs;
using ClimaSite.Application.Features.Cart.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OutputCaching;

namespace ClimaSite.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[OutputCache(NoStore = true)]  // Cart data is user-specific, don't cache
[Produces("application/json")]
public class CartController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly IGuestCartIdentity _guestCart;

    public CartController(IMediator mediator, IGuestCartIdentity guestCart)
    {
        _mediator = mediator;
        _guestCart = guestCart;
    }

    /// <summary>
    /// Get the current user's cart or guest cart.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(CartDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetCart([FromQuery] string? guestSessionId = null, [FromQuery] string lang = "en")
    {
        var guestId = await _guestCart.ResolveAsync(guestSessionId);
        var userId = GetUserId();
        var query = new GetCartQuery
        {
            UserId = userId,
            GuestSessionId = guestId,
            Language = lang
        };

        var result = await _mediator.Send(query);
        return Ok(result);
    }

    /// <summary>
    /// Add an item to the cart.
    /// </summary>
    [HttpPost("items")]
    [ProducesResponseType(typeof(CartDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> AddToCart([FromBody] AddToCartCommand command, [FromQuery] string lang = "en")
    {
        var guestId = await _guestCart.ResolveAsync(command.GuestSessionId);

        var result = await _mediator.Send(command with
        {
            Language = lang,
            GuestSessionId = guestId
        });
        if (!result.IsSuccess)
            return BadRequest(new { message = result.Error });

        return Ok(result.Value);
    }

    [HttpPut("items/{itemId:guid}")]
    public async Task<IActionResult> UpdateCartItem(
        Guid itemId,
        [FromBody] UpdateQuantityRequest request,
        [FromQuery] string lang = "en")
    {
        var guestId = await _guestCart.ResolveAsync(request.GuestSessionId);

        var command = new UpdateCartItemCommand
        {
            ItemId = itemId,
            Quantity = request.Quantity,
            GuestSessionId = guestId,
            Language = lang
        };

        var result = await _mediator.Send(command);
        if (!result.IsSuccess)
            return BadRequest(new { message = result.Error });

        return Ok(result.Value);
    }

    [HttpDelete("items/{itemId:guid}")]
    public async Task<IActionResult> RemoveFromCart(
        Guid itemId,
        [FromQuery] string? guestSessionId = null)
    {
        var guestId = await _guestCart.ResolveAsync(guestSessionId);

        var command = new RemoveFromCartCommand
        {
            ItemId = itemId,
            GuestSessionId = guestId
        };

        var result = await _mediator.Send(command);
        if (!result.IsSuccess)
            return BadRequest(new { message = result.Error });

        return Ok(new { message = "Item removed from cart" });
    }

    [HttpDelete]
    public async Task<IActionResult> ClearCart([FromQuery] string? guestSessionId = null)
    {
        var guestId = await _guestCart.ResolveAsync(guestSessionId);

        var command = new ClearCartCommand
        {
            GuestSessionId = guestId
        };

        var result = await _mediator.Send(command);
        if (!result.IsSuccess)
            return BadRequest(new { message = result.Error });

        return Ok(new { message = "Cart cleared" });
    }

    [Authorize]
    [HttpPost("merge")]
    public async Task<IActionResult> MergeGuestCart([FromQuery] string guestSessionId, [FromQuery] string lang = "en")
    {
        var guestId = await _guestCart.ResolveAsync(guestSessionId);

        var command = new MergeGuestCartCommand
        {
            GuestSessionId = guestId ?? string.Empty,
            Language = lang
        };

        var result = await _mediator.Send(command);
        if (!result.IsSuccess)
            return BadRequest(new { message = result.Error });

        return Ok(result.Value);
    }

    private Guid? GetUserId()
    {
        var userIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        return Guid.TryParse(userIdClaim, out var userId) ? userId : null;
    }
}

public record UpdateQuantityRequest
{
    public int Quantity { get; init; }
    public string? GuestSessionId { get; init; }
}
