using System.Threading;
using System.Threading.Tasks;

namespace Whitestone.SegnoSharp.Modules.Playlist
{
    public class PlaylistQueueLocker
    {
        private readonly SemaphoreSlim _queueMutex = new(1);

        public async Task LockQueueAsync()
        {
            await _queueMutex.WaitAsync();
        }

        public async Task LockQueueAsync(CancellationToken cancellationToken)
        {
            await _queueMutex.WaitAsync(cancellationToken);
        }

        public void UnlockQueue()
        {
            _queueMutex.Release();
        }
    }
}
