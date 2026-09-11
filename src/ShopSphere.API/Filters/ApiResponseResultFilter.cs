using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using ShopSphere.API.Models;

namespace ShopSphere.API.Filters;

public class ApiResponseResultFilter : IAlwaysRunResultFilter
{
    public void OnResultExecuting(ResultExecutingContext context)
    {
        if (context.Result is ObjectResult objectResult &&
            objectResult.StatusCode == StatusCodes.Status404NotFound)
        {
            context.Result = new NotFoundObjectResult(
                new ApiErrorResponse
                {
                    StatusCode = StatusCodes.Status404NotFound,
                    Message = "Resource not found."
                });
        }
    }

    public void OnResultExecuted(ResultExecutedContext context)
    {
    }
}