using BassService.Helpers;
using BassService.Interfaces;
using BassService.Models.Config;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Runtime.InteropServices;

namespace BassService.Extensions
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

            services.AddHostedService<BassServiceHost>();

            return services;
        }
    }
}
