using ClimaSite.Application.Features.Cart.Commands;
using ClimaSite.Application.Features.Cart.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OutputCaching;

namespace ClimaSite.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[OutputCache(NoStore = true)]  // Cart data is user-specific, don't cache
public class CartController : ControllerBase
{
    private readonly IMediator _mediator;

    public CartController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet]
    public async Task<IActionResult> GetCart([FromQuery] string? guestSessionId = null)
    {
        var userId = GetUserId();
        var query = new GetCartQuery
        {
            UserId = userId,
            GuestSessionId = guestSessionId
        };

        var result = await _mediator.Send(query);
        return Ok(result);
    }

    [HttpPost("items")]
    public async Task<IActionResult> AddToCart([FromBody] AddToCartCommand command)
    {
        var result = await _mediator.Send(command);
        if (!result.IsSuccess)
            return BadRequest(new { message = result.Error });

        return Ok(result.Value);
    }

    [HttpPut("items/{itemId:guid}")]
    public async Task<IActionResult> UpdateCartItem(
        Guid itemId,
        [FromBody] UpdateQuantityRequest request)
    {
        var command = new UpdateCartItemCommand
        {
            ItemId = itemId,
            Quantity = request.Quantity,
            GuestSessionId = request.GuestSessionId
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
        var command = new RemoveFromCartCommand
        {
            ItemId = itemId,
            GuestSessionId = guestSessionId
        };

        var result = await _mediator.Send(command);
        if (!result.IsSuccess)
            return BadRequest(new { message = result.Error });

        return Ok(new { message = "Item removed from cart" });
    }

    [HttpDelete]
    public async Task<IActionResult> ClearCart([FromQuery] string? guestSessionId = null)
    {
        var command = new ClearCartCommand
        {
            GuestSessionId = guestSessionId
        };

        var result = await _mediator.Send(command);
        if (!result.IsSuccess)
            return BadRequest(new { message = result.Error });

        return Ok(new { message = "Cart cleared" });
    }

    [Authorize]
    [HttpPost("merge")]
    public async Task<IActionResult> MergeGuestCart([FromQuery] string guestSessionId)
    {
        var command = new MergeGuestCartCommand
        {
            GuestSessionId = guestSessionId
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
