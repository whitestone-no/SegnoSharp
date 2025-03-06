using System;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Whitestone.SegnoSharp.Shared.Interfaces
{
    public interface IModule
    {
        Guid Id { get; }

        void ConfigureServices(IServiceCollection services, IHostEnvironment environment, IConfiguration configuration);
    }
}
