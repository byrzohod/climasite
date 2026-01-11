using ClimaSite.Application.Features.Wishlist.Commands;
using ClimaSite.Application.Features.Wishlist.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ClimaSite.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class WishlistController : ControllerBase
{
    private readonly IMediator _mediator;

    public WishlistController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet]
    public async Task<IActionResult> GetWishlist()
    {
        var result = await _mediator.Send(new GetWishlistQuery());
        return Ok(result);
    }

    [HttpPost("items/{productId:guid}")]
    public async Task<IActionResult> AddToWishlist(Guid productId)
    {
        var command = new AddToWishlistCommand { ProductId = productId };
        var result = await _mediator.Send(command);

        if (!result.IsSuccess)
            return BadRequest(new { message = result.Error });

        return Ok(new { message = "Added to wishlist" });
    }

    [HttpDelete("items/{productId:guid}")]
    public async Task<IActionResult> RemoveFromWishlist(Guid productId)
    {
        var command = new RemoveFromWishlistCommand { ProductId = productId };
        var result = await _mediator.Send(command);

        if (!result.IsSuccess)
            return BadRequest(new { message = result.Error });

        return Ok(new { message = "Removed from wishlist" });
    }
}
