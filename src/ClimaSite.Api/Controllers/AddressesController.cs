using ClimaSite.Application.Features.Addresses.Commands;
using ClimaSite.Application.Features.Addresses.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ClimaSite.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class AddressesController : ControllerBase
{
    private readonly IMediator _mediator;

    public AddressesController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet]
    public async Task<IActionResult> GetAddresses()
    {
        var result = await _mediator.Send(new GetUserAddressesQuery());
        return Ok(result);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetAddress(Guid id)
    {
        var result = await _mediator.Send(new GetAddressByIdQuery { AddressId = id });
        if (result == null)
            return NotFound();
        return Ok(result);
    }

    [HttpPost]
    public async Task<IActionResult> CreateAddress([FromBody] CreateAddressCommand command)
    {
        var result = await _mediator.Send(command);
        if (!result.IsSuccess)
            return BadRequest(new { message = result.Error });

        return CreatedAtAction(nameof(GetAddress), new { id = result.Value }, result.Value);
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> UpdateAddress(Guid id, [FromBody] UpdateAddressCommand command)
    {
        command.AddressId = id;
        var result = await _mediator.Send(command);
        if (!result.IsSuccess)
            return BadRequest(new { message = result.Error });

        return Ok(result.Value);
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> DeleteAddress(Guid id)
    {
        var result = await _mediator.Send(new DeleteAddressCommand { AddressId = id });
        if (!result.IsSuccess)
            return BadRequest(new { message = result.Error });

        return NoContent();
    }

    [HttpPut("{id:guid}/default")]
    public async Task<IActionResult> SetDefaultAddress(Guid id)
    {
        var result = await _mediator.Send(new SetDefaultAddressCommand { AddressId = id });
        if (!result.IsSuccess)
            return BadRequest(new { message = result.Error });

        return Ok(new { message = "Address set as default" });
    }
}
