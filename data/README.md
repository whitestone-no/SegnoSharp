This folder is used for data storage, like logs. This folder must be present for running SegnoSharp "out-of-the-box", and this README.MD file ensures that.
Any other files put into this folder will be ignored by Git.

# Special folders

## BASS

The `bass` folder must contain the BASS audio libraries. Download these from https://www.un4seen.com/bass.html

The following files must be present in this folder:

**All operating systems**
Bass.Net.dll (2.4.17.6)

**Windows**
bass.dll (2.4.17)
bassenc.dll (2.4.16.1)
bassenc_mp3.dll (2.4.1.6)
bassflac.dll (2.4.5.5)
bassmix.dll (2.4.12)

** Linux **
libbass.so (2.4.17)
libbassenc.so (2.4.16.1)
libbassenc_mp3.so (2.4.1.6)
libbassflac.so (2.4.5.5)
libbassmix.so (2.4.12)

Version numbers in parantheses are verified to work.

> Remember to use the `x64` versions of the assemblies!

## FFMPEG

FFMPEG is used as the encoder to convert the raw audio stream from BASS into a compressed format sent to the streaming server.
Download from https://www.ffmpeg.org/

** Windows **

- ffmpeg.exe

** Linux **

-- ffmpeg
