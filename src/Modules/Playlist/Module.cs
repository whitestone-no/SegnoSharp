using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System;
using Whitestone.SegnoSharp.Common.Interfaces;
using Whitestone.SegnoSharp.Modules.Playlist.Processors;

namespace Whitestone.SegnoSharp.Modules.Playlist
{
    public class Module : IModule
    {
        public Guid Id { get; } = Guid.NewGuid();

        public void ConfigureServices(IServiceCollection services, IHostEnvironment environment, IConfiguration configuration)
        {
            services.AddHostedService<PlaylistHandler>();
            services.AddSingleton<IPlaylistProcessor, DefaultProcessor>();
            services.AddSingleton<IPlaylistProcessor, AdvancedProcessor>();
            services.AddSingleton<IPlaylistProcessor, OtherProcessor>();
        }
    }
}
