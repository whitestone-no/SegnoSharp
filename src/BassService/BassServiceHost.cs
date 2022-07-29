﻿extern alias BassNetWindows;
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
            LoadAssemblies();

            if (!_bassWrapper.Initialize(0, 44100, (int)BASSInit.BASS_DEVICE_DEFAULT, IntPtr.Zero))
            {
                _log.LogCritical("Could not initialize BASS.NET: {0}", _bassWrapper.GetLastBassError());
            }

            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            if (!_bassWrapper.Uninitialize())
            {
                _log.LogError("Could not free BASS.NET resources: {0}", _bassWrapper.GetLastBassError());
            }

            UnloadAssemblies();

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
    }
}
