using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;

namespace Whitestone.SegnoSharp.Services;

public sealed class SecurityRolesSnapshotRefresher(SecurityRolesSnapshotProvider provider) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        await provider.ReloadAsync(ct);

        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(30));

        while (await timer.WaitForNextTickAsync(ct))
        {
            try
            {
                await provider.ReloadAsync(ct);
            }
            catch (OperationCanceledException)
            {
                break;
            }
        }
    }
}