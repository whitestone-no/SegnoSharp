using Whitestone.SegnoSharp.BassService.Helpers;
using Whitestone.SegnoSharp.BassService.Interfaces;
using Whitestone.SegnoSharp.BassService.Models.Config;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Whitestone.SegnoSharp.BassService.HealthChecks;
using Whitestone.SegnoSharp.Common.Interfaces;
using Whitestone.SegnoSharp.Common.Models.Configuration;

namespace Whitestone.SegnoSharp.BassService.Extensions
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddBassService(this IServiceCollection services, IConfiguration configuration)
        {
            services.Configure<BassRegistration>(configuration.GetSection(BassRegistration.Section));
            services.Configure<TagReaderConfig>(configuration.GetSection(TagReaderConfig.Section));

            services.AddSingleton<IBassWrapper, BassWrapper>();
            services.AddSingleton<ITagReader, TagReader>();

            services.AddHostedService<BassServiceHost>();

            services.AddHttpClient<StreamingServerHealthCheck>();
            services.AddHealthChecks().AddCheck<StreamingServerHealthCheck>("StreamingServer");

            return services;
        }
    }
}
