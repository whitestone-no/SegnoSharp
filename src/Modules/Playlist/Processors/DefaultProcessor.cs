using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Whitestone.SegnoSharp.Shared.Interfaces;
using Whitestone.SegnoSharp.Shared.Models;
using Whitestone.SegnoSharp.Database;
using Whitestone.SegnoSharp.Database.Models;
using Whitestone.SegnoSharp.Modules.Playlist.Models;

namespace Whitestone.SegnoSharp.Modules.Playlist.Processors
{
    public class DefaultProcessor(
        IRandomGenerator randomGenerator,
        IDbContextFactory<SegnoSharpDbContext> dbContextFactory) : IPlaylistProcessor
    {
        public string Name => "Default";
        public PlaylistProcessorSettings Settings { get; set; } = new DefaultProcessorSettings();

        public async Task<TrackStreamInfo> GetNextTrackAsync(CancellationToken cancellationToken)
        {
            await using SegnoSharpDbContext dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);

            // Only get tracks that are included in auto playlist
            // This can be a lot, but this is so far only the queryable.
            // Always order by the track id to ensure consistent results
            IQueryable<TrackStreamInfo> query = dbContext.TrackStreamInfos
                .AsNoTracking()
                .Where(t => t.IncludeInAutoPlaylist)
                .OrderBy(t => t.TrackId);

            if (Settings is DefaultProcessorSettings { UseWeightedRandom: false })
            {
                // For non-weighted random selection, get count and use skip
                // This uses the queryable and only gets the count
                int totalTracks = await query.CountAsync(cancellationToken);
                
                if (totalTracks == 0)
                {
                    return null;
                }

                int randomNumber = randomGenerator.GetInt(totalTracks);

                // Finally get the actual track. Everything above was performed in memory
                return await query
                    .Skip(randomNumber)
                    .FirstOrDefaultAsync(cancellationToken);
            }

            // For weighted random selection
            // This performs an actual query, but it only specifically gets the track id and weight so as efficient as possible
            var eligibleTracks = await query
                .Select(t => new { t.TrackId, t.Weight })
                .ToListAsync(cancellationToken);

            if (eligibleTracks.Count == 0){
            {
                return null;
            }}

            int weightSum = eligibleTracks.Sum(t => t.Weight);
            int rnd = randomGenerator.GetInt(weightSum);

            var runningTotal = 0;
            int selectedTrackId = eligibleTracks
                .Select(t => new { t.TrackId, RunningTotal = runningTotal += t.Weight })
                .First(t => t.RunningTotal >= rnd)
                .TrackId;

            // Finally get the actual track. Everything above was performed in memory
            return await dbContext.TrackStreamInfos
                .AsNoTracking()
                .FirstOrDefaultAsync(t => t.TrackId == selectedTrackId, cancellationToken);
        }
    }
}
