﻿using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System;
using Whitestone.SegnoSharp.Shared.Interfaces;
using Whitestone.SegnoSharp.Modules.Playlist.Models;
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
            services.AddSingleton<PlaylistSettings>();
            services.AddSingleton<PlaylistQueueLocker>();
        }
    }
}
