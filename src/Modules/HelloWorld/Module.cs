using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Whitestone.SegnoSharp.Common.Interfaces;

namespace HelloWorld
{
    public class Module : IModule
    {
        public string FriendlyName => "Hello SegnoSharp!";
        public int Priority => 100;

        public void ConfigureServices(IServiceCollection services, IHostEnvironment environment, IConfiguration configuration)
        {
        }
    }
}
