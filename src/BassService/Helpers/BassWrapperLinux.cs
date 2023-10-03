extern alias BassNetLinux;

using Whitestone.SegnoSharp.BassService.Interfaces;
using System;
using System.IO;
using System.Reflection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Whitestone.SegnoSharp.Common.Models;
using Whitestone.SegnoSharp.Common.Models.Configuration;

namespace Whitestone.SegnoSharp.BassService.Helpers
{
    public class BassWrapperLinux : IBassWrapper
    {
        private readonly ILogger<BassWrapperWindows> _log;
        private readonly StreamingServer _streamingServerConfig;
        private BassNetLinux::Un4seen.Bass.Misc.IBaseEncoder _encoder;
        private string _currentTitle = "SegnoSharp";

        public BassWrapperLinux(IOptions<StreamingServer> streamingServerConfig, ILogger<BassWrapperWindows> log)
        {
            _log = log;
            _streamingServerConfig = streamingServerConfig.Value;
        }
        public void Registration(string email, string key)
        {
            BassNetLinux::Un4seen.Bass.BassNet.Registration(email, key);
        }

        public bool Initialize(int device, int frequency, int flags, IntPtr win)
        {
            return BassNetLinux::Un4seen.Bass.Bass.BASS_Init(device, frequency, (BassNetLinux::Un4seen.Bass.BASSInit)flags, win);
        }

        public bool Uninitialize()
        {
            return BassNetLinux::Un4seen.Bass.Bass.BASS_Free();
        }

        public int CreateMixerStream(int frequency, int noOfChannels, int flags)
        {
            return BassNetLinux::Un4seen.Bass.AddOn.Mix.BassMix.BASS_Mixer_StreamCreate(frequency, noOfChannels, (BassNetLinux::Un4seen.Bass.BASSFlag)flags);
        }

        public int CreateFileStream(string file, long offset, long length, int flags)
        {
            // return BassNetLinux::Un4seen.Bass.Bass.BASS_StreamCreateFile(file, offset, length, (BassNetLinux::Un4seen.Bass.BASSFlag)flags);

            return BassNetLinux::Un4seen.Bass.AddOn.Flac.BassFlac.BASS_FLAC_StreamCreateFile(file, offset, length, (BassNetLinux::Un4seen.Bass.BASSFlag)flags);
        }

        public bool MixerAddStream(int mixerHandle, int streamHandle, int flags)
        {
            return BassNetLinux::Un4seen.Bass.AddOn.Mix.BassMix.BASS_Mixer_StreamAddChannel(mixerHandle, streamHandle, (BassNetLinux::Un4seen.Bass.BASSFlag)flags);
        }

        public bool FreeStream(int handle)
        {
            return BassNetLinux::Un4seen.Bass.Bass.BASS_StreamFree(handle);
        }

        public bool BassLoad(string folder)
        {
            return true;
            //return BassNetLinux::Un4seen.Bass.Bass.LoadMe(folder);
        }

        public bool BassLoadEnc(string folder)
        {
            return true;
            //return BassNetWindows::Un4seen.Bass.AddOn.Enc.BassEnc.LoadMe(folder);
        }

        public bool BassLoadMixer(string folder)
        {
            return true;
            //return BassNetLinux::Un4seen.Bass.AddOn.Mix.BassMix.
        }

        public bool BassLoadFlac(string folder)
        {
            return true;
            //return BassNetLinux::Un4seen.Bass.AddOn.Flac.BassFlac
        }

        public bool BassUnload()
        {
            return true;
            //return BassNetLinux::Un4seen.Bass.Bass.FreeMe();
        }

        public bool BassUnloadEnc()
        {
            return true;
            //return BassNetLinux::Un4seen.Bass.AddOn.Enc.BassEnc.FreeMe();
        }

        public bool BassUnloadMixer()
        {
            return true;
            //return BassNetLinux::Un4seen.Bass.AddOn.Mix.BassMix.FreeMe();
        }

        public bool BassUnloadFlac()
        {
            return true;
            //return BassNetLinux::Un4seen.Bass.AddOn.Flac.BassFlac.FreeMe();
        }

        public Models.Bass.BASSError GetLastBassError()
        {
            return Enum.TryParse(BassNetLinux::Un4seen.Bass.Bass.BASS_ErrorGetCode().ToString(), out Models.Bass.BASSError outValue) ? outValue : Models.Bass.BASSError.BASS_ERROR_UNKNOWN;
        }

        public Version GetBassVersion()
        {
            return BassNetLinux::Un4seen.Bass.Bass.BASS_GetVersion(4);
        }

        public Version GetBassEncVersion()
        {
            return BassNetLinux::Un4seen.Bass.AddOn.Enc.BassEnc.BASS_Encode_GetVersion(4);
        }

        public Version GetBassMixerVersion()
        {
            return BassNetLinux::Un4seen.Bass.AddOn.Mix.BassMix.BASS_Mixer_GetVersion(4);
        }

        public bool Play(int handle, bool restart)
        {
            return BassNetLinux::Un4seen.Bass.Bass.BASS_ChannelPlay(handle, restart);
        }

        public bool MixerPlay(int streamHandle)
        {
            return BassNetLinux::Un4seen.Bass.AddOn.Mix.BassMix.BASS_Mixer_ChannelPlay(streamHandle);
        }

        public bool Stop(int handle)
        {
            return BassNetLinux::Un4seen.Bass.Bass.BASS_ChannelStop(handle);
        }

        public bool SlideAttribute(int handle, int attribute, float value, int time)
        {
            return BassNetLinux::Un4seen.Bass.Bass.BASS_ChannelSlideAttribute(handle, (BassNetLinux::Un4seen.Bass.BASSAttribute)attribute, value, time);
        }

        public Tags GetTagFromFile(string file)
        {
            int stream = CreateFileStream(file, 0L, 0L, (int)BassNetLinux::Un4seen.Bass.BASSFlag.BASS_DEFAULT);

            BassNetLinux::Un4seen.Bass.AddOn.Tags.TAG_INFO tags = new BassNetLinux::Un4seen.Bass.AddOn.Tags.TAG_INFO(file);
            if (BassNetLinux::Un4seen.Bass.AddOn.Tags.BassTags.BASS_TAG_GetFromFile(stream, tags))
            {
                _ = ushort.TryParse(tags.year, out ushort year);
                _ = byte.TryParse(tags.disc, out byte disc);
                _ = ushort.TryParse(tags.track, out ushort trackNo);

                var tagsFromFile = new Tags
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

                if (tags.PictureCount > 0)
                {
                    BassNetLinux::Un4seen.Bass.AddOn.Tags.TagPicture tagPicture = tags.PictureGet(0);
                    if (tagPicture != null &&
                        tagPicture.PictureStorage == BassNetLinux::Un4seen.Bass.AddOn.Tags.TagPicture.PICTURE_STORAGE.Internal &&
                        tagPicture.PictureType == BassNetLinux::Un4seen.Bass.AddOn.Tags.TagPicture.PICTURE_TYPE.FrontAlbumCover)
                    {
                        tagsFromFile.CoverImage = new TagsImage
                        {
                            MimeType = tagPicture.MIMEType,
                            Data = tagPicture.Data
                        };
                    }
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

            BassNetLinux::Un4seen.Bass.Misc.EncoderCMDLN encoder = new BassNetLinux::Un4seen.Bass.Misc.EncoderCMDLN(channel)
            {
                EncoderDirectory = encoderPath,
                CMDLN_Executable = "ffmpeg",
                CMDLN_CBRString = "-f s16le -ar 44100 -ac 2 -i ${input} -c:a mp3 -b:a ${kbps}k -vn -f mp3 ${output}", // Remember to use "-f adts" for AAC streaming
                CMDLN_EncoderType = BassNetLinux::Un4seen.Bass.BASSChannelType.BASS_CTYPE_STREAM_MP3,
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

            bool castInitSuccess = BassNetLinux::Un4seen.Bass.AddOn.Enc.BassEnc.BASS_Encode_CastInit(
                encoder.EncoderHandle,
                _streamingServerConfig.Address + ":" + _streamingServerConfig.Port + _streamingServerConfig.MountPoint,
                _streamingServerConfig.Password,
                BassNetLinux::Un4seen.Bass.AddOn.Enc.BassEnc.BASS_ENCODE_TYPE_MP3,
                _streamingServerConfig.Name,
                _streamingServerConfig.ServerUrl,
                _streamingServerConfig.Genre,
                _streamingServerConfig.Description,
                null,
                0,
                _streamingServerConfig.IsPublic
            );

            if (!castInitSuccess)
            {
                _log.LogCritical("Could not start casting. {error}", GetLastBassError());
            }
            else
            {
                _log.LogDebug("Casting to {server} started", _streamingServerConfig.Address + ":" + _streamingServerConfig.Port + _streamingServerConfig.MountPoint);
            }

            if (!BassNetLinux::Un4seen.Bass.AddOn.Enc.BassEnc.BASS_Encode_CastSetTitle(encoder.EncoderHandle, _currentTitle, null))
            {
                _log.LogWarning("Could not update title on streaming server. {error}", GetLastBassError());
            }
        }

        // This method must be duplicated between Windows and Linux implementations of IBassWrapper
        // because they use interfaces defined in two different DLLs. Therefore implementation
        // can't be shared between the two implementations and they must have their own.
        public void StopStreaming()
        {
            if (_encoder != null && _encoder.IsActive)
            {
                if (!_encoder.Stop())
                {
                    _log.LogError("Failed to stop encoder: {0}", GetLastBassError());
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
                if (!BassNetLinux::Un4seen.Bass.AddOn.Enc.BassEnc.BASS_Encode_CastSetTitle(_encoder.EncoderHandle, _currentTitle, null))
                {
                    _log.LogWarning("Could not update title on streaming server");
                }
            }
        }
    }
}
