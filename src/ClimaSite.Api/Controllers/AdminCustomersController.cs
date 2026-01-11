using ClimaSite.Application.Features.Admin.Customers.Commands;
using ClimaSite.Application.Features.Admin.Customers.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ClimaSite.Api.Controllers;

[ApiController]
[Route("api/admin/customers")]
[Authorize(Roles = "Admin")]
public class AdminCustomersController : ControllerBase
{
    private readonly IMediator _mediator;

    public AdminCustomersController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet]
    public async Task<IActionResult> GetCustomers([FromQuery] GetAdminCustomersQuery query)
    {
        var result = await _mediator.Send(query);
        return Ok(result);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetCustomer(Guid id)
    {
        var result = await _mediator.Send(new GetAdminCustomerByIdQuery { Id = id });
        if (result == null)
        {
            return NotFound(new { message = "Customer not found" });
        }
        return Ok(result);
    }

    [HttpPut("{id:guid}/status")]
    public async Task<IActionResult> UpdateCustomerStatus(Guid id, [FromBody] UpdateCustomerStatusRequest request)
    {
        var command = new UpdateCustomerStatusCommand
        {
            CustomerId = id,
            IsActive = request.IsActive
        };

        var result = await _mediator.Send(command);
        if (!result.IsSuccess)
        {
            return BadRequest(new { message = result.Error });
        }
        return NoContent();
    }
}

public record UpdateCustomerStatusRequest
{
    public bool IsActive { get; init; }
}
