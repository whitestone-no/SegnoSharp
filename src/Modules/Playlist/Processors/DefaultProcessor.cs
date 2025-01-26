using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Whitestone.SegnoSharp.Common.Attributes.PersistenceManager;
using Whitestone.SegnoSharp.Common.Interfaces;
using Whitestone.SegnoSharp.Common.Models;
using Whitestone.SegnoSharp.Database;
using Whitestone.SegnoSharp.Database.Models;

namespace Whitestone.SegnoSharp.Modules.Playlist.Processors
{
    public class DefaultProcessor(
        IRandomGenerator randomGenerator,
        IDbContextFactory<SegnoSharpDbContext> dbContextFactory) : IPlaylistProcessor
    {
        public PlaylistProcessorSettings Settings { get; set; } = new DefaultProcessorSettings();

        public async Task<TrackStreamInfo> GetNextTrackAsync(CancellationToken cancellationToken)
        {
            // EFCore doesn't have support for window functions yet, so in order to get a running total need to get all
            // rows and then do the running total calculation (instead of using SUM OVER)

            await using SegnoSharpDbContext dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);

            int weightsSum = await dbContext.TrackStreamInfos
                .AsNoTracking()
                .Where(t => t.IncludeInAutoPlaylist)
                .SumAsync(t => t.Weight, cancellationToken);
            int rnd = randomGenerator.GetInt(weightsSum);

            var runningTotal = 0;

            List<TrackStreamInfo> list = await dbContext.TrackStreamInfos
                .AsNoTracking()
                .Where(t => t.IncludeInAutoPlaylist)
                .ToListAsync(cancellationToken);

            var track = list
                .Select(ts => new
                {
                    TrackStreamInfo = ts,
                    RunningTotal = runningTotal += ts.Weight
                })
                .FirstOrDefault(ts => ts.RunningTotal >= rnd);

            return track?.TrackStreamInfo;
        }
    }

    public class DefaultProcessorSettings : PlaylistProcessorSettings
    {
        [Persist]
        [DefaultValue(true)]
        public override bool Enabled { get; set; }

        [Persist]
        [DefaultValue(false)]
        public bool UseWeightedRandom { get; set; }
    }
}
