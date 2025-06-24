using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Primitives;

namespace DevHabit.Api.Services.Idempotency;

[AttributeUsage(AttributeTargets.Method)]
public sealed class IdempotentRequestAttribute : Attribute, IAsyncActionFilter
{
    private const string IdempotencyKeyHeader = "Idempotency-Key";
    private static readonly TimeSpan DefaultCacheDuration = TimeSpan.FromMinutes(60);
    
    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        if (!context.HttpContext.Request.Headers.TryGetValue(IdempotencyKeyHeader, out StringValues idempotencyKeyValue) ||
            !Guid.TryParse(idempotencyKeyValue, out Guid idempotenceKey))
        {
            ProblemDetailsFactory problemDetailsFactory =
                context.HttpContext.RequestServices.GetRequiredService<ProblemDetailsFactory>();

            ProblemDetails problemDetails = problemDetailsFactory.CreateProblemDetails(
                context.HttpContext,
                statusCode: StatusCodes.Status400BadRequest, 
                title: "Bad Request",
                detail: $"Invalid or missing {IdempotencyKeyHeader} header");
            
            context.Result = new BadRequestObjectResult(problemDetails);
            return;
        }
        
        IMemoryCache memoryCache = context.HttpContext.RequestServices.GetRequiredService<IMemoryCache>();
        string cacheKey = $"idempotence:{idempotenceKey}";
        
        int? statusCode = memoryCache.Get<int?>(cacheKey);
        if (statusCode != null)
        {
            var result = new StatusCodeResult(statusCode.Value);
            context.Result = result;
            
            return;
        }
        
        ActionExecutedContext actionExecutedContext = await next();
        
        if (actionExecutedContext.Result is ObjectResult objectResult)
        {
            memoryCache.Set(cacheKey, objectResult.StatusCode, DefaultCacheDuration);
        }
    }
}
