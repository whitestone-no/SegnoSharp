using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Whitestone.SegnoSharp.Common.Interfaces;

namespace Whitestone.SegnoSharp.Playlist.Extensions
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddPlaylistHandler(this IServiceCollection services, IConfiguration configuration)
        {
            services.AddSingleton<IPlaylistHandler, PlaylistHandler>();
            services.AddHostedService(provider => provider.GetRequiredService<IPlaylistHandler>());

            return services;
        }
    }
}
