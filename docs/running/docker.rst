Docker
------

The easiest way to run SegnoSharp is through the official Docker image.

::

    docker pull ghcr.io/whitestone-no/segnosharp:latest


Prerequisites
=============

Before starting the Docker container you need to perform a few additional steps.

First and foremost, SegnoSharp expects a ``data`` folder to be mapped as a volume.
This is where logs and the SQLite database will be stored.
It can be any existing folder on your harddrive or a new and empty one.

BASS
====

SegnoSharp uses the BASS audio libraries to play and stream music.
Create a new folder inside the ``data`` folder called ``bass``.

Download the libraries from `un4seen <https://www.un4seen.com/bass.html/>`_ and extract the files into ``data/bass``.
The following table contains a list of all libraries and which file to extract into ``data/bass``

+-------------+----------------------+----------------------------+------------------+
| Library     | Filename             | Extract                    | Verified version |
+=============+======================+============================+==================+
| BASS        | bass24-linux.zip     | libs/x86_64/libbass.so     | 2.4.17           |
+-------------+----------------------+----------------------------+------------------+
| BASSFLAC    | bassflac24-linux.zip | libs/x86_64/libbassflac.so | 2.4.5.5          |
+-------------+----------------------+----------------------------+------------------+
| BASSmix     | bassmix24-linux.zip  | libs/x86_64/libbassmix.so  | 2.4.12           |
+-------------+----------------------+----------------------------+------------------+
| BASSenc     | bassenc24-linux.zip  | libs/x86_64/libbassenc.so  | 2.4.16.1         |
+-------------+----------------------+----------------------------+------------------+
| Bass.Net    | Bass24.Net.zip       | core/Bass.Net.dll          | 2.4.17.6         |
+-------------+----------------------+----------------------------+------------------+

The ``data/bass`` folder should now contain the following files:

- Bass.Net.dll
- libbass.so
- libbassenc.so
- libbassflac.so
- libbassmix.so

FFMPEG
======

SegnoSharp uses FFMPEG to encode the raw audio from BASS into a compressed stream that BASS then streams to Shoutcast/Icecast.
Create a new folder inside the ``data`` folder called ``ffmpeg``, and download a linux build for linux64/amd64 and extract the ``ffmpeg`` binary
from the downloaded package into ``data/ffmpeg``. Find a suitable package at `FFMPEG <https://www.ffmpeg.org/>`_.

Starting the container
======================

The ``data`` folder needs to be mapped into the container as a volume.
You must also map a folder containing your music files into the container, otherwise SegnoSharp won't be able to find anything to play.

In order to access the web page in the container you also need to forward a network port.

And finally, in order for SegnoSharp to work properly you need to set some configuration values as environment variables.

All this is rather cumbersome to do in a single command. Even though it can be done it is recommended to use `Docker Compose <https://docs.docker.com/compose/>`_ as that gives you a bit more flexibility and ease of use.

Create a new file called ``docker-compose.yml`` with the following content:

::

    services:
      segnosharp:
        container_name: segnosharp
        image: ghcr.io/gthvidsten/segnosharp:latest
        volumes:
          - "pathToYourDataFolder:/var/segnosharp"
          - "pathToYourMusicFolder:/var/music"
        ports:
          - "8080:8080"
        environment:
          SegnoSharp_CommonConfig__DataPath: /var/segnosharp
          SegnoSharp_CommonConfig__MusicPath: /var/music
		  SegnoSharp_OpenIdConnect__UseOidc:false

Replace ``pathToYourDataFolder`` and ``pathToYourMusicFolder`` with the real paths from your computer.
This file defines a Docker service called ``segnosharp`` with two folders on your computer mapped into the container.
It also defines the available network ports, in this case port ``8080`` in the container is mapped to port ``8080`` on your computer.
Finally it sets a few environment variables into the container. All settings are described in another chapter TODO, but these three are required as a minimum for testing the application.

.. note:: ``UseOidc`` should never be set to ``false`` in a production environment! Setting this to false overrides all security measures!

After you have created this file and updated the settings accordingly you can now start the Docker container:

::

    docker-compose up -d
	
This will download (pull) the image if you have not already done it, and start the image as a container.
When it says ``Completed`` and returns you to the command line you should be able to start using SegnoSharp on `http://localhost:8080 <http://localhost:8080>`_.

Test
