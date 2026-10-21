using FluentValidation;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using ShopSphere.API.Filters;

namespace ShopSphere.UnitTests.Filters;

public class ValidationFilterTests
{
    [Fact]
    public async Task OnActionExecutionAsync_InvalidRequest_ReturnsBadRequestAndSkipsAction()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddSingleton<IValidator<TestRequest>, TestRequestValidator>();

        using var serviceProvider = services.BuildServiceProvider();

        var filter = new ValidationFilter(serviceProvider);
        var httpContext = new DefaultHttpContext
        {
            RequestServices = serviceProvider
        };

        var actionContext = new ActionContext(
            httpContext,
            new RouteData(),
            new ActionDescriptor());

        var request = new TestRequest { Name = "" };

        var context = new ActionExecutingContext(
            actionContext,
            new List<IFilterMetadata>(),
            new Dictionary<string, object?> { ["request"] = request },
            controller: new object());

        var actionExecuted = false;

        ActionExecutionDelegate next = () =>
        {
            actionExecuted = true;

            var executedContext = new ActionExecutedContext(
                actionContext,
                new List<IFilterMetadata>(),
                controller: new object());

            return Task.FromResult(executedContext);
        };

        // Act
        await filter.OnActionExecutionAsync(context, next);

        // Assert
        var result = Assert.IsType<BadRequestObjectResult>(context.Result);
        Assert.Equal(StatusCodes.Status400BadRequest, result.StatusCode);
        Assert.False(actionExecuted);
    }

    [Fact]
    public async Task OnActionExecutionAsync_ValidRequest_ContinuesToAction()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddSingleton<IValidator<TestRequest>, TestRequestValidator>();

        using var serviceProvider = services.BuildServiceProvider();

        var filter = new ValidationFilter(serviceProvider);
        var httpContext = new DefaultHttpContext
        {
            RequestServices = serviceProvider
        };

        var actionContext = new ActionContext(
            httpContext,
            new RouteData(),
            new ActionDescriptor());

        var context = new ActionExecutingContext(
            actionContext,
            new List<IFilterMetadata>(),
            new Dictionary<string, object?>
            {
                ["request"] = new TestRequest { Name = "Valid name" }
            },
            controller: new object());

        var actionExecuted = false;

        ActionExecutionDelegate next = () =>
        {
            actionExecuted = true;

            return Task.FromResult(
                new ActionExecutedContext(
                    actionContext,
                    new List<IFilterMetadata>(),
                    controller: new object()));
        };

        // Act
        await filter.OnActionExecutionAsync(context, next);

        // Assert
        Assert.True(actionExecuted);
        Assert.Null(context.Result);
    }

    [Fact]
    public async Task OnActionExecutionAsync_NoRegisteredValidator_ContinuesToAction()
    {
        // Arrange
        using var serviceProvider = new ServiceCollection()
            .BuildServiceProvider();

        var filter = new ValidationFilter(serviceProvider);
        var httpContext = new DefaultHttpContext
        {
            RequestServices = serviceProvider
        };

        var actionContext = new ActionContext(
            httpContext,
            new RouteData(),
            new ActionDescriptor());

        var context = new ActionExecutingContext(
            actionContext,
            new List<IFilterMetadata>(),
            new Dictionary<string, object?>
            {
                ["request"] = new UnvalidatedRequest()
            },
            controller: new object());

        var actionExecuted = false;

        ActionExecutionDelegate next = () =>
        {
            actionExecuted = true;

            return Task.FromResult(
                new ActionExecutedContext(
                    actionContext,
                    new List<IFilterMetadata>(),
                    controller: new object()));
        };

        // Act
        await filter.OnActionExecutionAsync(context, next);

        // Assert
        Assert.True(actionExecuted);
        Assert.Null(context.Result);
    }

    private sealed class TestRequest
    {
        public string Name { get; set; } = string.Empty;
    }

    private sealed class UnvalidatedRequest;

    private sealed class TestRequestValidator
        : AbstractValidator<TestRequest>
    {
        public TestRequestValidator()
        {
            RuleFor(x => x.Name)
                .NotEmpty()
                .WithMessage("Name is required.");
        }
    }
}
