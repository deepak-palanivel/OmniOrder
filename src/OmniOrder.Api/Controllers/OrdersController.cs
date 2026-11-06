using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OmniOrder.Api.Middleware;
using OmniOrder.Application.DTOs;
using OmniOrder.Application.Interfaces;

namespace OmniOrder.Api.Controllers;

[ApiController]
[Route("api/v1/orders")]
public class OrdersController : ControllerBase
{
    private readonly IOrderService _orderService;

    public OrdersController(IOrderService orderService)
    {
        _orderService = orderService;
    }

    [HttpPost]
    [Idempotent]
    public async Task<IActionResult> CreateOrder([FromBody] CreateOrderRequest request, CancellationToken ct)
    {
        var result = await _orderService.CreateOrderAsync(request, ct);
        return CreatedAtAction(nameof(GetOrder), new { id = result.Id }, result);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetOrder(Guid id, CancellationToken ct)
    {
        var order = await _orderService.GetOrderByIdAsync(id, ct);
        return order == null ? NotFound() : Ok(order);
    }

    [HttpGet]
    public async Task<IActionResult> GetAllOrders(CancellationToken ct)
    {
        var orders = await _orderService.GetAllOrdersAsync(ct);
        return Ok(orders);
    }
}
