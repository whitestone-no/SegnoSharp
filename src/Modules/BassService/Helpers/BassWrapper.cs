using System;
using Microsoft.Extensions.Logging;
using Un4seen.Bass;
using Un4seen.Bass.AddOn.Enc;
using Un4seen.Bass.AddOn.EncMp3;
using Un4seen.Bass.AddOn.Mix;
using Un4seen.Bass.AddOn.Tags;
using Whitestone.SegnoSharp.Common.Models;
using Whitestone.SegnoSharp.Modules.BassService.Interfaces;

namespace Whitestone.SegnoSharp.Modules.BassService.Helpers
{
    public class BassWrapper(ILogger<BassWrapper> log) : IBassWrapper
    {

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

        public bool SetAttribute(int handle, BASSAttribute attribute, float value)
        {
            return Bass.BASS_ChannelSetAttribute(handle, attribute, value);
        }

        public bool SlideAttribute(int handle, BASSAttribute attribute, float value, int time)
        {
            return Bass.BASS_ChannelSlideAttribute(handle, attribute, value, time);
        }

        public int SetSync(int handle, BASSSync type, long param, SYNCPROC proc)
        {
            return Bass.BASS_ChannelSetSync(handle, type, param, proc, IntPtr.Zero);
        }

        public bool RemoveSync(int handle, int sync)
        {
            return Bass.BASS_ChannelRemoveSync(handle, sync);
        }

        // TODO: Move this to a separate module?
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

        public bool CastInit(int handle, string server, string pass, string content, string name, string url, string genre, string desc, string headers, int bitrate, BASSEncodeCast flags)
        {
            return BassEnc.BASS_Encode_CastInit(handle, server, pass, content, name, url, genre, desc, headers, bitrate, flags);
        }

        public bool SetStreamingTitle(int handle, string title)
        {
            return BassEnc.BASS_Encode_CastSetTitle(handle, title, null);
        }
    }
}