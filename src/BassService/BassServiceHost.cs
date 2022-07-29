extern alias BassNetWindows;
using System;
using BassService.Interfaces;
using BassService.Models.Config;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.IO;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using BassNetWindows::Un4seen.Bass;
using SYNCPROC = BassService.Models.Bass.SYNCPROC;

namespace BassService
{
    public class BassServiceHost : IHostedService
    {
        private readonly IBassWrapper _bassWrapper;
        private readonly ILogger<BassServiceHost> _log;

        private int _mixer;
        private SYNCPROC _mixerStallSync;

        public BassServiceHost(IBassWrapper bassWrapper, IOptions<BassRegistration> bassRegistration, ILogger<BassServiceHost> log)
        {
            _bassWrapper = bassWrapper;
            _log = log;

            _bassWrapper.Registration(bassRegistration.Value.Email, bassRegistration.Value.Key);
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            try
            {
                LoadAssemblies();

                // Initialize BASS
                if (!_bassWrapper.Initialize(0, 44100, (int)BASSInit.BASS_DEVICE_DEFAULT, IntPtr.Zero))
                {
                    _log.LogCritical("Could not initialize BASS.NET: {0}", _bassWrapper.GetLastBassError());
                }

                // Initialize BASS Mixer
                _mixer = _bassWrapper.CreateMixerStream(44100, 2, (int)(BASSFlag.BASS_SAMPLE_FLOAT | BASSFlag.BASS_MIXER_NONSTOP));
                if (_mixer == 0)
                {
                    _log.LogError("Failed to create mixer: {0}", _bassWrapper.GetLastBassError());
                }

                _mixerStallSync = OnMixerStall;
                if (_bassWrapper.AddSynchronizer(_mixer, (int)BASSSync.BASS_SYNC_STALL, 0L, _mixerStallSync, IntPtr.Zero) == 0)
                {
                    _log.LogError("Failed to attach 'stall' event handler: {0}", _bassWrapper.GetLastBassError());
                }

                // Start playback of BASS Mixer
                if (!_bassWrapper.Play(_mixer, false))
                {
                    _log.LogError("Failed to play mixer: {0}", _bassWrapper.GetLastBassError());
                }
            }
            catch (Exception e)
            {
                _log.LogError(e, $"Unknown exception during {nameof(BassServiceHost)} startup");
            }

            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            try
            {
                // Stop playback of BASS Mixer
                if (!_bassWrapper.Stop(_mixer))
                {
                    _log.LogError("Could not stop mixer: {0}", _bassWrapper.GetLastBassError());
                }

                // Uninitialize BASS Mixer
                if (!_bassWrapper.FreeStream(_mixer))
                {
                    _log.LogError("Failed to free mixer: {0}", _bassWrapper.GetLastBassError());
                }

                // Uninitialize BASS
                if (!_bassWrapper.Uninitialize())
                {
                    _log.LogError("Could not free BASS.NET resources: {0}", _bassWrapper.GetLastBassError());
                }

                UnloadAssemblies();
            }
            catch (Exception e)
            {
                _log.LogError(e, $"Unknown exception during {nameof(BassServiceHost)} shutdown");
            }

            return Task.CompletedTask;
        }

        private void LoadAssemblies()
        {
            string executingFolder = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            string libraryPath = Path.Combine(executingFolder ?? Directory.GetCurrentDirectory(), "basslibwin");

            if (!_bassWrapper.BassLoad(libraryPath))
            {
                // Don't use GetLastBassError here as Bass hasn't been loaded
                _log.LogCritical("Could not load BASS from {0}", libraryPath);
            }

            if (!_bassWrapper.BassLoadEnc(libraryPath))
            {
                _log.LogCritical("Could not load bassenc.dll from {0}", libraryPath);
            }

            if (!_bassWrapper.BassLoadEncMp3(libraryPath))
            {
                _log.LogCritical("Could not load bassenc_mp3.dll from {0}", libraryPath);
            }

            if (!_bassWrapper.BassLoadMixer(libraryPath))
            {
                _log.LogCritical("Could not load bassmix.dll from {0}", libraryPath);
            }

            if (!_bassWrapper.BassLoadFlac(libraryPath))
            {
                _log.LogCritical("Could not load bassflac.dll from {0}", libraryPath);
            }

            _log.LogInformation("BASS Version: {0}", _bassWrapper.GetBassVersion());
            _log.LogInformation("BASS Enc Version: {0}", _bassWrapper.GetBassEncVersion());
            _log.LogInformation("BASS Mixer Version: {0}", _bassWrapper.GetBassMixerVersion());
        }

        private void UnloadAssemblies()
        {
            // Unload the BASS DLLs in the reverse order than they were loaded in.
            // No need to ensure unload with an if as they will always be unloaded when the application ends.
            _bassWrapper.BassUnloadFlac();
            _bassWrapper.BassUnloadMixer();
            _bassWrapper.BassUnloadEncMp3();
            _bassWrapper.BassUnloadEnc();
            _bassWrapper.BassUnload();
        }

        private void OnMixerStall(int handle, int channel, int data, IntPtr user)
        {
            _log.LogDebug("MIXER STALL CALLED");
        }

    }
}
