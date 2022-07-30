using Whitestone.WASP.BassService.Helpers;
using Whitestone.WASP.BassService.Interfaces;
using Whitestone.WASP.BassService.Models.Config;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Runtime.InteropServices;
using Whitestone.WASP.Common.Interfaces;

namespace Whitestone.WASP.BassService.Extensions
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddBassService(this IServiceCollection services, IConfiguration configuration)
        {
            services.Configure<BassRegistration>(configuration.GetSection(BassRegistration.Section));

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                services.AddSingleton<IBassWrapper, BassWrapperLinux>();
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                services.AddSingleton<IBassWrapper, BassWrapperWindows>();
            }
            else
            {
                throw new Exception("Unsupported Operating System");
            }

            services.AddSingleton<ITagReader, TagReader>();

            services.AddHostedService<BassServiceHost>();

            return services;
        }
    }
}
