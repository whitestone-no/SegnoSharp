using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Whitestone.SegnoSharp.Common.Interfaces
{
    public interface IModule
    {
        void ConfigureServices(IServiceCollection services, IHostEnvironment environment, IConfiguration configuration);
    }
}
