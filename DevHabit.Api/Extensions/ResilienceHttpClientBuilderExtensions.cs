using Microsoft.Extensions.Http.Resilience;

namespace DevHabit.Api.Extensions;

public static class ResilienceHttpClientBuilderExtensions
{
    public static IHttpClientBuilder InternalRemoveAllResilienceHandlers(this IHttpClientBuilder builder)
    {
        builder.ConfigureAdditionalHttpMessageHandlers((handlers, _) =>
        {
            for (int index = handlers.Count - 1; index >= 0; --index)
            {
                if (handlers[index] is ResilienceHandler)
                {
                    handlers.RemoveAt(index);
                }
            }
        });
        return builder;
    }
}
