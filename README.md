# SegnoSharp

## Purpose
To create a simple audio streaming tool that can randomly stream audio files to Shoutcast/Icecast. Meant to replace SAM Broadcaster for internal internet radio.

## Usage

In order to use this project you manually have to download the BASS libraries, as they are closed source and cannot be distributed in this repository. Make sure to download the x64 variant of both Windows and Linux libraries. Download the following from http://www.un4seen.com/

- Main BASS library
- BASSFLAC
- BASSmix
- BASSenc_MP3
- Bass.Net

Put the .dll and .so files in the corresponding "windows" and "linux" folders inside the "lib/BASS" folder. Each folder also includes a list of files it should contain.

Put your registration email and key in the User Secrets file, and/or create an `.env` file if you intend to use Docker Compose. The `.env` file is ignored by Git so it is same to put sensitive data in it (unless the `.gitignore` file is edited to include it).

### Example User Secrets

```
{
    "BASS:Registration:Key" : "xxxx",
    "BASS:Registration:Email" : "yyyy@zzzz.aaa"
}
```

### Example .env

```
SegnoSharp_BASS__Registration__Key=xxxx
SegnoSharp_BASS__Registration__Email=yyyy@zzzz.aaa	
```

## Database

SegnoSharp uses SQLite by default. The connection string can be seen in `appsettings.json`. The database filename referenced by default will be stored in the data folder.

SegnoSharp also supports MySQL, so change `Database:Type` to `mysql`, and place your connection string in the User Secrets file, and/or in the `.env` file. This is an example connection string for MySQL: `server=serverIpOrDns;uid=dbuser;pwd=dbpassword;database=SegnoSharp`