using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Whitestone.SegnoSharp.Common.Interfaces;

namespace HelloWorld
{
    public class Module : IModule
    {
        public void ConfigureServices(IServiceCollection services, IHostEnvironment environment, IConfiguration configuration)
        {
        }
    }
}
