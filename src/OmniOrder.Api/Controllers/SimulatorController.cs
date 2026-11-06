using Microsoft.AspNetCore.Mvc;
using OmniOrder.Application.Interfaces;

namespace OmniOrder.Api.Controllers;

[ApiController]
[Route("api/v1/simulator")]
public class SimulatorController : ControllerBase
{
    private readonly IOrderService _orderService;

    public SimulatorController(IOrderService orderService)
    {
        _orderService = orderService;
    }

    /// <summary>
    /// Spawns 50 parallel async tasks attempting to buy the last N units of a product.
    /// Proves optimistic concurrency: exactly N succeed, rest get 409 Conflict.
    /// </summary>
    [HttpPost("flash-sale")]
    public async Task<IActionResult> SimulateFlashSale(
        [FromQuery] Guid? productId,
        [FromQuery] int buyers = 50,
        [FromQuery] int stock = 5,
        CancellationToken ct = default)
    {
        // Default to the Flash Sale GPU product
        var targetProductId = productId ?? Guid.Parse("a1b2c3d4-e5f6-7890-abcd-ef1234567890");
        var result = await _orderService.SimulateFlashSaleAsync(targetProductId, buyers, stock, ct);
        return Ok(result);
    }
}
