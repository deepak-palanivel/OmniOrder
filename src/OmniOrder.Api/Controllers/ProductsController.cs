using Microsoft.AspNetCore.Mvc;
using OmniOrder.Application.Interfaces;

namespace OmniOrder.Api.Controllers;

[ApiController]
[Route("api/v1/products")]
public class ProductsController : ControllerBase
{
    private readonly IInventoryService _inventoryService;

    public ProductsController(IInventoryService inventoryService)
    {
        _inventoryService = inventoryService;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll(CancellationToken ct)
    {
        var products = await _inventoryService.GetAllProductsAsync(ct);
        return Ok(products);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
    {
        var product = await _inventoryService.GetProductByIdAsync(id, ct);
        return product == null ? NotFound() : Ok(product);
    }
}
