using Whitestone.SegnoSharp.BassService.Interfaces;
using System;
using System.IO;
using System.Reflection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Un4seen.Bass;
using Un4seen.Bass.AddOn.Enc;
using Un4seen.Bass.AddOn.Flac;
using Un4seen.Bass.AddOn.Mix;
using Un4seen.Bass.AddOn.Tags;
using Un4seen.Bass.Misc;
using Whitestone.SegnoSharp.Common.Models;
using StreamingServer = Whitestone.SegnoSharp.Common.Models.Configuration.StreamingServer;
using Un4seen.Bass.AddOn.EncMp3;

namespace Whitestone.SegnoSharp.BassService.Helpers
{
    public class BassWrapper : IBassWrapper
    {
        private readonly ILogger<BassWrapper> _log;
        private readonly StreamingServer _streamingServerConfig;
        private IBaseEncoder _encoder;
        private string _currentTitle = "SegnoSharp";

        public BassWrapper(IOptions<StreamingServer> streamingServerConfig, ILogger<BassWrapper> log)
        {
            _log = log;
            _streamingServerConfig = streamingServerConfig.Value;
        }

        public void Registration(string email, string key)
        {
            BassNet.Registration(email, key);
        }

        public bool Initialize(int device, int frequency, BASSInit flags, IntPtr win)
        {
            return Bass.BASS_Init(device, frequency, flags, win);
        }

        public bool Uninitialize()
        {
            return Bass.BASS_Free();
        }

        public int CreateMixerStream(int frequency, int noOfChannels, BASSFlag flags)
        {
            return BassMix.BASS_Mixer_StreamCreate(frequency, noOfChannels, flags);
        }

        public int CreateFileStream(string file, long offset, long length, BASSFlag flags)
        {
            return Bass.BASS_StreamCreateFile(file, offset, length, flags);

            //return BassFlac.BASS_FLAC_StreamCreateFile(file, offset, length, flags);
        }

        public bool MixerAddStream(int mixerHandle, int streamHandle, BASSFlag flags)
        {
            return BassMix.BASS_Mixer_StreamAddChannel(mixerHandle, streamHandle, flags);
        }

        public bool FreeStream(int handle)
        {
            return Bass.BASS_StreamFree(handle);
        }

        public int BassLoadPlugin(string plugin)
        {
            return Bass.BASS_PluginLoad(plugin);
        }

        public bool BassUnloadPlugins()
        {
            return Bass.BASS_PluginFree(0);
        }

        public BASSError GetLastBassError()
        {
            return Bass.BASS_ErrorGetCode();
        }

        public Version GetBassVersion()
        {
            return Bass.BASS_GetVersion(4);
        }

        public Version GetBassEncVersion()
        {
            return BassEnc.BASS_Encode_GetVersion(4);
        }

        public Version GetBassEncMp3Version()
        {
            return BassEnc_Mp3.BASS_Encode_MP3_GetVersion(4);
        }

        public Version GetBassMixerVersion()
        {
            return BassMix.BASS_Mixer_GetVersion(4);
        }

        public Version GetBassNetVersion()
        {
            return Utils.GetVersion();
        }

        public bool Play(int handle, bool restart)
        {
            return Bass.BASS_ChannelPlay(handle, restart);
        }

        public bool MixerPlay(int streamHandle)
        {
            return BassMix.BASS_Mixer_ChannelPlay(streamHandle);
        }

        public bool Stop(int handle)
        {
            return Bass.BASS_ChannelStop(handle);
        }

        public bool SlideAttribute(int handle, BASSAttribute attribute, float value, int time)
        {
            return Bass.BASS_ChannelSlideAttribute(handle, attribute, value, time);
        }

        public Tags GetTagFromFile(string file)
        {
            int stream = CreateFileStream(file, 0L, 0L, (int)BASSFlag.BASS_DEFAULT);

            TAG_INFO tags = new(file);
            if (BassTags.BASS_TAG_GetFromFile(stream, tags))
            {
                _ = ushort.TryParse(tags.year, out ushort year);
                _ = byte.TryParse(tags.disc, out byte disc);
                _ = ushort.TryParse(tags.track, out ushort trackNo);

                Tags tagsFromFile = new()
                {
                    Album = tags.album,
                    AlbumArtist = tags.albumartist,
                    Artist = tags.artist,
                    Composer = tags.composer,
                    Disc = disc,
                    Duration = tags.duration,
                    Genre = tags.genre,
                    Title = tags.title,
                    TrackNumber = trackNo,
                    Year = year
                };

                if (tags.PictureCount <= 0)
                {
                    return tagsFromFile;
                }
                
                TagPicture tagPicture = tags.PictureGet(0);
                if (tagPicture is { PictureStorage: TagPicture.PICTURE_STORAGE.Internal, PictureType: TagPicture.PICTURE_TYPE.FrontAlbumCover })
                {
                    tagsFromFile.CoverImage = new TagsImage
                    {
                        MimeType = tagPicture.MIMEType,
                        Data = tagPicture.Data
                    };
                }

                return tagsFromFile;
            }

            _log.LogWarning("Could not read tags from {file}", file);
            return null;
        }

        // This method must be duplicated between Windows and Linux implementations of IBassWrapper
        // because they use interfaces defined in two different DLLs. Therefore implementation
        // can't be shared between the two implementations and they must have their own.
        public void StartStreaming(int channel)
        {
            if (_encoder != null)
            {
                return;
            }

            string executingFolder = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            string encoderPath = Path.Combine(executingFolder ?? Directory.GetCurrentDirectory(), "encoder");

            EncoderCMDLN encoder = new(channel)
            {
                EncoderDirectory = encoderPath,
                CMDLN_Executable = "ffmpeg.exe",
                CMDLN_CBRString = "-f s16le -ar 44100 -ac 2 -i ${input} -c:a mp3 -b:a ${kbps}k -vn -f mp3 ${output}", // Remember to use "-f adts" for AAC streaming
                CMDLN_EncoderType = BASSChannelType.BASS_CTYPE_STREAM_MP3,
                CMDLN_DefaultOutputExtension = ".mp3",
                CMDLN_Bitrate = 320,
                CMDLN_SupportsSTDOUT = true,
                CMDLN_ParamSTDIN = "-",
                CMDLN_ParamSTDOUT = "-"
            };

            if (encoder.EncoderExists)
            {
                _encoder = encoder;
                _log.LogDebug("BASS Encoder is set up with the following command line: {commandLine}", encoder.EncoderCommandLine);
            }
            else
            {
                _log.LogCritical("Could not find FFMPEG in {0}", encoder.EncoderDirectory);
            }

            if (!encoder.Start(null, IntPtr.Zero, false))
            {
                _log.LogCritical("Could not start encoder");
            }
            else
            {
                _log.LogDebug("Encoder started");
            }

            bool castInitSuccess = BassEnc.BASS_Encode_CastInit(
                encoder.EncoderHandle,
                _streamingServerConfig.Address + ":" + _streamingServerConfig.Port + _streamingServerConfig.MountPoint,
                _streamingServerConfig.Password,
                BassEnc.BASS_ENCODE_TYPE_MP3,
                _streamingServerConfig.Name,
                _streamingServerConfig.ServerUrl,
                _streamingServerConfig.Genre,
                _streamingServerConfig.Description,
                null,
                0,
                _streamingServerConfig.IsPublic ? BASSEncodeCast.BASS_ENCODE_CAST_PUBLIC : BASSEncodeCast.BASS_ENCODE_CAST_DEFAULT
                );

            if (!castInitSuccess)
            {
                _log.LogCritical("Could not start casting. {error}", GetLastBassError());
            }
            else
            {
                _log.LogDebug("Casting to {server} started", _streamingServerConfig.Address + ":" + _streamingServerConfig.Port + _streamingServerConfig.MountPoint);
            }

            if (!BassEnc.BASS_Encode_CastSetTitle(encoder.EncoderHandle, _currentTitle, null))
            {
                _log.LogWarning("Could not update title on streaming server. {error}", GetLastBassError());
            }
        }

        // This method must be duplicated between Windows and Linux implementations of IBassWrapper
        // because they use interfaces defined in two different DLLs. Therefore implementation
        // can't be shared between the two implementations and they must have their own.
        public void StopStreaming()
        {
            if (_encoder is { IsActive: true })
            {
                if (!_encoder.Stop())
                {
                    _log.LogError("Failed to stop encoder: {error}", GetLastBassError());
                }
                else
                {
                    _log.LogDebug("Encoder stopped");
                }
            }
            _encoder = null;
        }

        public void SetStreamingTitle(string title)
        {
            _currentTitle = title;

            if (_encoder != null)
            {
                if (!BassEnc.BASS_Encode_CastSetTitle(_encoder.EncoderHandle, _currentTitle, null))
                {
                    _log.LogWarning("Could not update title on streaming server");
                }
            }
        }
    }
}
