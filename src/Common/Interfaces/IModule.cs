using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Whitestone.SegnoSharp.Common.Interfaces
{
    public interface IModule
    {
        string FriendlyName { get; }
        int Priority { get; }

        void ConfigureServices(IServiceCollection services, IHostEnvironment environment, IConfiguration configuration);
    }
}
