using Microsoft.AspNetCore.Mvc;
using ShopSphere.Application.Features.Products.Requests;
using ShopSphere.Application.Features.Products.Services;

namespace ShopSphere.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ProductsController : ControllerBase
{
    private readonly IProductService _productService;

    public ProductsController(IProductService productService)
    {
        _productService = productService;
    }

    [HttpPost]
    public async Task<ActionResult> Create(
        CreateProductRequest request,
        CancellationToken cancellationToken)
    {
        var product = await _productService.CreateAsync(
            request,
            cancellationToken);

        return Ok(product);
    }

    [HttpGet]
    public async Task<ActionResult> GetAll(
        CancellationToken cancellationToken)
    {
        var products = await _productService.GetAllAsync(
            cancellationToken);

        return Ok(products);
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult> GetById(
        int id,
        CancellationToken cancellationToken)
    {
        var product = await _productService.GetByIdAsync(
            id,
            cancellationToken);

        if (product is null)
        {
            return NotFound();
        }

        return Ok(product);
    }

    [HttpPut("{id:int}")]
    public async Task<ActionResult> Update(
        int id,
        UpdateProductRequest request,
        CancellationToken cancellationToken)
    {
        var product = await _productService.UpdateAsync(
            id,
            request,
            cancellationToken);

        if (product is null)
        {
            return NotFound();
        }

        return Ok(product);
    }

    [HttpDelete("{id:int}")]
    public async Task<ActionResult> Delete(
        int id,
        CancellationToken cancellationToken)
    {
        var deleted = await _productService.DeleteAsync(
            id,
            cancellationToken);

        if (!deleted)
        {
            return NotFound();
        }

        return NoContent();
    }

    [HttpPost("{id:int}/restore")]
    public async Task<ActionResult> Restore(
        int id,
        CancellationToken cancellationToken)
    {
        var product = await _productService.RestoreAsync(
            id,
            cancellationToken);

        if (product is null)
        {
            return NotFound();
        }

        return Ok(product);
    }
}