
using ShopSphere.Application.Features.Products.Requests;
using ShopSphere.Application.Features.Products.Validators;
using Xunit;

namespace ShopSphere.UnitTests.Products;

public class ProductQueryRequestValidatorTests
{
    private readonly ProductQueryRequestValidator _validator = new();

    [Fact]
    public void Validate_DefaultRequest_ShouldBeValid()
    {
        var request = new ProductQueryRequest();

        var result = _validator.Validate(request);

        Assert.True(result.IsValid);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Validate_InvalidPageNumber_ShouldFail(int pageNumber)
    {
        var request = new ProductQueryRequest
        {
            PageNumber = pageNumber
        };

        var result = _validator.Validate(request);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors,
            error => error.PropertyName == nameof(request.PageNumber));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(101)]
    [InlineData(-1)]
    public void Validate_InvalidPageSize_ShouldFail(int pageSize)
    {
        var request = new ProductQueryRequest
        {
            PageSize = pageSize
        };

        var result = _validator.Validate(request);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors,
            error => error.PropertyName == nameof(request.PageSize));
    }

    [Theory]
    [InlineData(-1)]
    public void Validate_NegativeCategoryId_ShouldFail(int categoryId)
    {
        var request = new ProductQueryRequest
        {
            CategoryId = categoryId
        };

        var result = _validator.Validate(request);

        Assert.False(result.IsValid);
    }

    [Fact]
    public void Validate_MinPriceGreaterThanMaxPrice_ShouldFail()
    {
        var request = new ProductQueryRequest
        {
            MinPrice = 1000,
            MaxPrice = 500
        };

        var result = _validator.Validate(request);

        Assert.False(result.IsValid);
    }

    [Fact]
    public void Validate_UnsupportedSortField_ShouldFail()
    {
        var request = new ProductQueryRequest
        {
            SortBy = "InvalidField"
        };

        var result = _validator.Validate(request);

        Assert.False(result.IsValid);
    }
}
