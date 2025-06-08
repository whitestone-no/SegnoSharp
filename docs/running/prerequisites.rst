#############
Prerequisites
#############

The :ref:`quickstart guide <refQuickstart>` contains all the necessary prerequisites, but we'll briefly go through them here again, and include some other details.

***********
Data folder
***********

You must have a data folder where SegnoSharp can store its logs, and databse if you are running with SQLite.

**********
Lib folder
**********

The Docker image comes with a predefined ``lib`` folder, so there's no need to perform the steps below if you are using this.

.. _refPrerequisiteBass:

BASS
====

SegnoSharp uses the BASS audio libraries to play and stream music.
Create a new folder inside the ``lib`` folder called ``bass``.

Download the libraries from `un4seen <https://www.un4seen.com/bass.html>`_ and extract the files into ``lib/bass``.
The following table contains a list of all libraries and which file to extract into ``lib/bass``

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

The ``lib/bass`` folder should now contain the following files:

- Bass.Net.dll
- libbass.so
- libbassenc.so
- libbassflac.so
- libbassmix.so

.. note:: If you are going to run SegnoSharp in a Windows environment you will need the (64 bit) ``Win32`` version of these libraries. See the table below for details:

+-------------+----------------+-------------------+------------------+
| Library     | Filename       | Extract           | Verified version |
+=============+================+===================+==================+
| BASS        | bass24.zip     | x64/bass.dll      | 2.4.17           |
+-------------+----------------+-------------------+------------------+
| BASSFLAC    | bassflac24.zip | x64/bassflac.dll  | 2.4.5.5          |
+-------------+----------------+-------------------+------------------+
| BASSmix     | bassmix24.zip  | x64/bassmix.dll   | 2.4.12           |
+-------------+----------------+-------------------+------------------+
| BASSenc     | bassenc24.zip  | x64/bassenc.dll   | 2.4.16.1         |
+-------------+----------------+-------------------+------------------+
| Bass.Net    | Bass24.Net.zip | core/Bass.Net.dll | 2.4.17.6         |
+-------------+----------------+-------------------+------------------+

FFMPEG
======

SegnoSharp uses FFMPEG to encode the raw audio from BASS into a compressed stream that BASS then streams to Shoutcast/Icecast.
Create a new folder inside the ``lib`` folder called ``ffmpeg``, and download a linux build for linux64/amd64 and extract the ``ffmpeg`` binary
from the downloaded package into ``lib/ffmpeg``. Find a suitable package at `FFMPEG <https://www.ffmpeg.org/>`_.

If you plan on running SegnoSharp on Windows you need to download a Windows distribution of FFMPEG and place ``ffmpeg.exe``
in the ``ffmpeg`` folder inside the ``lib`` folder.

***********
Source code
***********

If you want to run this through .NET or Visual Studio you need to clone the source code repository:

::

    git clone https://github.com/whitestone-no/SegnoSharp
