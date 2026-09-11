using Microsoft.AspNetCore.Mvc;
using ShopSphere.API.Models;
using ShopSphere.Application.Features.Categories.DTOs;
using ShopSphere.Application.Features.Categories.Requests;
using ShopSphere.Application.Features.Categories.Services;

namespace ShopSphere.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class CategoriesController : ControllerBase
{
    private readonly ICategoryService _categoryService;

    public CategoriesController(ICategoryService categoryService)
    {
        _categoryService = categoryService;
    }

    [HttpPost]
    [ProducesResponseType(typeof(CategoryDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult> Create(
            CreateCategoryRequest request,
        CancellationToken cancellationToken)
    {
        var category = await _categoryService.CreateAsync(
            request,
            cancellationToken);

        return Ok(category);
    }

    [HttpGet]
    [ProducesResponseType(typeof(List<CategoryDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult> GetAll(
            CancellationToken cancellationToken)
    {
        var categories = await _categoryService.GetAllAsync(
            cancellationToken);

        return Ok(categories);
    }

    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(CategoryDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<ActionResult> GetById(
        int id,
        CancellationToken cancellationToken)
    {
        var category = await _categoryService.GetByIdAsync(
            id,
            cancellationToken);

        if (category is null)
        {
            return NotFound();
        }

        return Ok(category);
    }

    [HttpPut("{id:int}")]
    [ProducesResponseType(typeof(CategoryDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult> Update(
        int id,
        UpdateCategoryRequest request,
        CancellationToken cancellationToken)
    {
        var category = await _categoryService.UpdateAsync(
            id,
            request,
            cancellationToken);

        if (category is null)
        {
            return NotFound();
        }

        return Ok(category);
    }

    [HttpDelete("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<ActionResult> Delete(
        int id,
        CancellationToken cancellationToken)
    {
        var deleted = await _categoryService.DeleteAsync(
            id,
            cancellationToken);

        if (!deleted)
        {
            return NotFound();
        }

        return NoContent();
    }

    [HttpPost("{id:int}/restore")]
    [ProducesResponseType(typeof(CategoryDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<ActionResult> Restore(
        int id,
        CancellationToken cancellationToken)
    {
        var category = await _categoryService.RestoreAsync(
            id,
            cancellationToken);

        if (category is null)
        {
            return NotFound();
        }

        return Ok(category);
    }
}