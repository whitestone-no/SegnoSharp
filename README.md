# SegnoSharp

![SegnoSharp logo](https://raw.githubusercontent.com/gthvidsten/SegnoSharp/main/img/logo.png)

A simple audio streaming tool that can randomly stream audio files to Shoutcast/Icecast.

## Usage

In order to use this project you manually have to download some files. See the `README.md` file in the `data` folder

### BASS
In addition the files mentioned above you will also need to register for a BASS license.

Create two configuration values called `BASS:Registration:Key` and `BASS:Registration:Email`. These can be placed in the User Secrets for the BassService module, or create an `.env` file in the `src` folder if you intend to use Docker Compose. Both User Secrets and the `.env` file are ignored by Git so it is safe to put sensitive data in them (unless the `.gitignore` file is edited to include it).

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

SegnoSharp also supports MySQL, so change `Database:Type` to `mysql`, and place your connection string in the User Secrets for SegnoSharp, or in the `.env` file. This is an example connection string for MySQL: `server=serverIpOrDns;uid=dbuser;pwd=dbpassword;database=SegnoSharp`
