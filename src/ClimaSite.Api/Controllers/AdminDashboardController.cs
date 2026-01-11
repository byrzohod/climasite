using ClimaSite.Application.Features.Admin.Dashboard.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ClimaSite.Api.Controllers;

[ApiController]
[Route("api/admin/dashboard")]
[Authorize(Roles = "Admin")]
public class AdminDashboardController : ControllerBase
{
    private readonly IMediator _mediator;

    public AdminDashboardController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet]
    public async Task<IActionResult> GetDashboardKpis()
    {
        var result = await _mediator.Send(new GetDashboardKpisQuery());
        return Ok(result);
    }

    [HttpGet("revenue")]
    public async Task<IActionResult> GetRevenueChart([FromQuery] string period = "7d")
    {
        var result = await _mediator.Send(new GetRevenueChartQuery { Period = period });
        return Ok(result);
    }

    [HttpGet("orders-chart")]
    public async Task<IActionResult> GetOrderStatusChart()
    {
        var result = await _mediator.Send(new GetOrderStatusChartQuery());
        return Ok(result);
    }

    [HttpGet("recent-orders")]
    public async Task<IActionResult> GetRecentOrders([FromQuery] int count = 10)
    {
        var result = await _mediator.Send(new GetRecentOrdersQuery { Count = count });
        return Ok(result);
    }

    [HttpGet("low-stock")]
    public async Task<IActionResult> GetLowStockProducts([FromQuery] int count = 10)
    {
        var result = await _mediator.Send(new GetLowStockProductsQuery { Count = count });
        return Ok(result);
    }

    [HttpGet("top-products")]
    public async Task<IActionResult> GetTopSellingProducts([FromQuery] int count = 10, [FromQuery] string period = "30d")
    {
        var result = await _mediator.Send(new GetTopSellingProductsQuery { Count = count, Period = period });
        return Ok(result);
    }
}
