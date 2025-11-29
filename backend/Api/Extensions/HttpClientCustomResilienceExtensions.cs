using Api.Interfaces;
using Api.Service;
using Microsoft.Extensions.Http.Resilience;
using Polly;
using Polly.Retry;

namespace Api.Extensions
{   
    // U Program.cs, umesto AddHttpClient<...>.Add
    public static class HttpClientCustomResilienceExtensions
    {
        public static IServiceCollection AddFMPHttpClientWithCustomResilience(this IServiceCollection services)
        {
            services.AddHttpClient<IFinacialModelingPrepService, FinancialModelingPrepService>()
                    .AddResilienceHandler("fmp-resilience", (builder, context) =>
                {

                    // Retry
                    builder.AddRetry(new HttpRetryStrategyOptions
                    {
                        MaxRetryAttempts = 3,
                        Delay = TimeSpan.FromSeconds(2),
                        UseJitter = true,
                        OnRetry = args =>
                        {
                            
                            return default;
                        }
                    });

                    // Timeout
                    builder.AddTimeout(new HttpTimeoutStrategyOptions
                    {
                        Timeout = TimeSpan.FromSeconds(5),
                        OnTimeout = args =>
                        {
                            
                            return default;
                        }
                    });

                    // Fallback
                    // Nesto nije htelo pa sam odustao od ovoga
                });

            return services;
        }
    }
}
