extern alias BassNetWindows;
using System;
using BassService.Interfaces;
using BassService.Models.Config;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using BassNetWindows::Un4seen.Bass;

namespace BassService
{
    public class BassServiceHost : IHostedService
    {
        private readonly IBassWrapper _bassWrapper;
        private readonly ILogger<BassServiceHost> _log;

        public BassServiceHost(IBassWrapper bassWrapper, IOptions<BassRegistration> bassRegistration, ILogger<BassServiceHost> log)
        {
            _bassWrapper = bassWrapper;
            _log = log;

            _bassWrapper.Registration(bassRegistration.Value.Email, bassRegistration.Value.Key);
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            string executingFolder = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            string libraryPath = Path.Combine(executingFolder ?? Directory.GetCurrentDirectory(), "basslibwin");

            if (!_bassWrapper.BassLoad(libraryPath))
            {
                // Don't use GetLastBassError here as Bass hasn't been loaded
                _log.LogCritical("Could not load BASS from {0}", libraryPath);
            }

            if (!_bassWrapper.BassLoadMixer(libraryPath))
            {
                _log.LogCritical("Could not load bassmix.dll from {0}", libraryPath);
            }

            if (!_bassWrapper.BassLoadFlac(libraryPath))
            {
                _log.LogCritical("Could not load bassflac.dll from {0}", libraryPath);
            }

            if (!_bassWrapper.Initialize(0, 44100, (int) BASSInit.BASS_DEVICE_DEFAULT, IntPtr.Zero))
            {
                _log.LogCritical("Could not initialize BASS.NET: {0}", _bassWrapper.GetLastBassError());
            }

            _log.LogInformation("BASS Version: {0:X8}", _bassWrapper.GetBassVersion());
            _log.LogInformation("BASS Mixer Version: {0:X8}", _bassWrapper.GetBassMixerVersion());

            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }
    }
}
