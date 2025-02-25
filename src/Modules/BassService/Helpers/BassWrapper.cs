using System;
using System.IO;
using System.Runtime.InteropServices;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Un4seen.Bass;
using Un4seen.Bass.AddOn.Enc;
using Un4seen.Bass.AddOn.EncMp3;
using Un4seen.Bass.AddOn.Mix;
using Un4seen.Bass.AddOn.Tags;
using Un4seen.Bass.Misc;
using Whitestone.SegnoSharp.Common.Models;
using Whitestone.SegnoSharp.Common.Models.Configuration;
using Whitestone.SegnoSharp.Modules.BassService.Interfaces;
using Whitestone.SegnoSharp.Modules.BassService.Models.Config;

namespace Whitestone.SegnoSharp.Modules.BassService.Helpers
{
    public class BassWrapper(
        IOptions<Ffmpeg> ffmpegConfig,
        IOptions<CommonConfig> commonConfig,
        ILogger<BassWrapper> log)
        : IBassWrapper
    {
        private readonly Ffmpeg _ffmpegConfig = ffmpegConfig.Value;
        private readonly CommonConfig _commonConfig = commonConfig.Value;
        private IBaseEncoder _encoder;
        private string _currentTitle = "SegnoSharp";

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
                    Year = year,
                    Notes = tags.comment
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

            log.LogWarning("Could not read tags from {file}", file);
            return null;
        }

        public void StartStreaming(int channel, StreamingSettings settings)
        {
            if (_encoder != null)
            {
                return;
            }

            string encoderPath = Path.Combine(_commonConfig.DataPath, _ffmpegConfig.DataFolder);

            var ffmpegExecutable = "ffmpeg";
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                ffmpegExecutable = "ffmpeg.exe";
            }

            var format1 = "mp3";
            var format2 = "mp3";
            var extension = ".mp3";
            var encoderCtype = BASSChannelType.BASS_CTYPE_STREAM_MP3;
            string encoderType = BassEnc.BASS_ENCODE_TYPE_MP3;
            if (settings.AudioFormat == AudioFormat.Aac)
            {
                format1 = "aac";
                format2 = "adts";
                extension = ".aac";
                encoderCtype = BASSChannelType.BASS_CTYPE_STREAM_AAC;
                encoderType = BassEnc.BASS_ENCODE_TYPE_AAC;
            }

            EncoderCMDLN encoder = new(channel)
            {
                EncoderDirectory = encoderPath,
                CMDLN_Executable = ffmpegExecutable,
                CMDLN_CBRString = "-f s16le -ar 44100 -ac 2 -i ${input} -c:a " + format1 + " -b:a ${kbps}k -vn -f " + format2 + " ${output}", // Remember to use "-f adts" for AAC streaming
                CMDLN_EncoderType = encoderCtype,
                CMDLN_DefaultOutputExtension = extension,
                CMDLN_Bitrate = (int)settings.Bitrate,
                CMDLN_SupportsSTDOUT = true,
                CMDLN_ParamSTDIN = "-",
                CMDLN_ParamSTDOUT = "-"
            };

            if (encoder.EncoderExists)
            {
                _encoder = encoder;
                log.LogDebug("BASS Encoder is set up with the following command line: {commandLine}", encoder.EncoderCommandLine);
            }
            else
            {
                log.LogCritical("Could not find FFMPEG in {0}", encoder.EncoderDirectory);
            }

            if (!encoder.Start(null, IntPtr.Zero, false))
            {
                log.LogCritical("Could not start encoder");
            }
            else
            {
                log.LogDebug("Encoder started");
            }

            bool castInitSuccess = BassEnc.BASS_Encode_CastInit(
                encoder.EncoderHandle,
                settings.Hostname + ":" + settings.Port + settings.MountPoint,
                settings.Password,
                encoderType,
                settings.Name,
                settings.ServerUrl,
                settings.Genre,
                settings.Description,
                null,
                0,
                settings.IsPublic ? BASSEncodeCast.BASS_ENCODE_CAST_PUBLIC : BASSEncodeCast.BASS_ENCODE_CAST_DEFAULT
                );

            if (!castInitSuccess)
            {
                log.LogCritical("Could not start casting. {error}", GetLastBassError());
            }
            else
            {
                log.LogDebug("Casting to {server} started", settings.Hostname + ":" + settings.Port + settings.MountPoint);
            }

            if (!BassEnc.BASS_Encode_CastSetTitle(encoder.EncoderHandle, _currentTitle, null))
            {
                log.LogWarning("Could not update title on streaming server. {error}", GetLastBassError());
            }
        }

        public void StopStreaming()
        {
            if (_encoder is { IsActive: true })
            {
                if (!_encoder.Stop())
                {
                    log.LogError("Failed to stop encoder: {error}", GetLastBassError());
                }
                else
                {
                    log.LogDebug("Encoder stopped");
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
                    log.LogWarning("Could not update title on streaming server");
                }
            }
        }
    }
}
