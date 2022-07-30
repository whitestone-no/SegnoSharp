using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Whitestone.WASP.Common.Interfaces;

namespace Whitestone.WASP.Playlist.Extensions
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddPlaylistHandler(this IServiceCollection services, IConfiguration configuration)
        {
            services.AddSingleton<IPlaylistHandler, PlaylistHandler>();

            return services;
        }
    }
}
