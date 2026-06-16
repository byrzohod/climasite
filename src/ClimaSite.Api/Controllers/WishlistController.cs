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
    public async Task<IActionResult> GetWishlist([FromQuery] string lang = "en")
    {
        var result = await _mediator.Send(new GetWishlistQuery { Language = lang });
        return Ok(result);
    }

    [HttpGet("shared/{shareToken}")]
    [AllowAnonymous]
    public async Task<IActionResult> GetSharedWishlist(string shareToken, [FromQuery] string lang = "en")
    {
        var result = await _mediator.Send(new GetSharedWishlistQuery { ShareToken = shareToken, Language = lang });
        return result == null ? NotFound() : Ok(result);
    }

    [HttpPost("items/{productId:guid}")]
    public async Task<IActionResult> AddToWishlist(Guid productId, [FromQuery] string lang = "en")
    {
        var command = new AddToWishlistCommand { ProductId = productId, Language = lang };
        var result = await _mediator.Send(command);

        if (!result.IsSuccess)
            return BadRequest(new { message = result.Error });

        return Ok(result.Value);
    }

    [HttpDelete("items/{productId:guid}")]
    public async Task<IActionResult> RemoveFromWishlist(Guid productId)
    {
        var command = new RemoveFromWishlistCommand { ProductId = productId };
        var result = await _mediator.Send(command);

        if (!result.IsSuccess)
            return BadRequest(new { message = result.Error });

        return Ok(result.Value);
    }

    [HttpDelete]
    public async Task<IActionResult> ClearWishlist()
    {
        var result = await _mediator.Send(new ClearWishlistCommand());

        if (!result.IsSuccess)
            return BadRequest(new { message = result.Error });

        return Ok(result.Value);
    }

    [HttpPut("share")]
    public async Task<IActionResult> SetSharing([FromBody] SetWishlistSharingRequest request)
    {
        var result = await _mediator.Send(new SetWishlistSharingCommand
        {
            IsPublic = request.IsPublic
        });

        if (!result.IsSuccess)
            return BadRequest(new { message = result.Error });

        return Ok(result.Value);
    }

    [HttpPost("share-token")]
    public async Task<IActionResult> RegenerateShareToken()
    {
        var result = await _mediator.Send(new RegenerateWishlistShareTokenCommand());

        if (!result.IsSuccess)
            return BadRequest(new { message = result.Error });

        return Ok(result.Value);
    }
}

public record SetWishlistSharingRequest(bool IsPublic);
