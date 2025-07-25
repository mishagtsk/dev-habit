using Microsoft.AspNetCore.Http.Json;
using Microsoft.Extensions.Options;

namespace DevHabit.Api.Extensions;

public static class ContentNegotiationResultExtensions
{
    public static IResult OkWithContentNegotiation<T>(this IResultExtensions _, T obj)
    {
        return new OnContentNegotiation<T>(obj);
    }

    private sealed class OnContentNegotiation<TValue> : IResult, IStatusCodeHttpResult, IValueHttpResult,
        IValueHttpResult<TValue>
    {
        internal OnContentNegotiation(TValue value)
        {
            Value = value;
        }
        
        public TValue? Value { get; }
        
        object? IValueHttpResult.Value => Value;
        
        private static int StatusCode => StatusCodes.Status200OK;

        int? IStatusCodeHttpResult.StatusCode => StatusCode;
        
        public async Task ExecuteAsync(HttpContext httpContext)
        {
            ArgumentNullException.ThrowIfNull(httpContext);

            httpContext.Response.StatusCode = StatusCode;

            await httpContext.Response.WriteAsJsonAsync(Value,
                options: httpContext.RequestServices.GetService<IOptions<JsonOptions>>()?.Value.SerializerOptions,
                contentType: httpContext.Request.Headers.Accept.ToString());
        }
    }
}
