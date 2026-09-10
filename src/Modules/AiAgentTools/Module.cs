using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System;
using Whitestone.SegnoSharp.Modules.AiAgentTools.Services;
using Whitestone.SegnoSharp.Shared.Interfaces;

namespace Whitestone.SegnoSharp.Modules.AiAgentTools;

public class Module : IModule, IMcpProvider
{
    public Guid Id { get; } = Guid.NewGuid();
    public string McpPrefix => "playlist_tools";

    public void ConfigureServices(IServiceCollection services, IHostEnvironment environment, IConfiguration configuration)
    {
        services.AddScoped<IMusicSearchService, MusicSearchService>();
    }
}