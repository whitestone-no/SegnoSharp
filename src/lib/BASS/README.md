# WASP
Whitestone Audio Streaming Project

## Purpose
To create a simple audio streaming tool that can randomly stream audio files to Shoutcast. Meant to replace SAM Broadcaster for internal internet radio.

## Usage

In order to use this project you manually have to download the BASS libraries, as they are closed source and cannot be distributed in this repository. Make sure to download the x64 variant of both Windows and Linux libraries. Download the following from http://www.un4seen.com/

- Main BASS library
- BASSFLAC
- BASSmix
- BASSenc_MP3
- Bass.Net

Put the .dll and .so files in the corresponding "windows" and "linux" folders inside the "lib" folder.

Put your registration email and key in the User Secrets file, and/or create an `.env` file if you intend to use Docker Compose.

### Example User Secrets

```
{
    "BASS:Registration:Key" : "xxxx",
    "BASS:Registration:Email" : "yyyy@zzzz.aaa"
}
```

### Example .env

```
WASP_BASS__Registration__Key=xxxx
WASP_BASS__Registration__Email=yyyy@zzzz.aaa	
```
