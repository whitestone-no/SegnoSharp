#############
Prerequisites
#############

The :ref:`quickstart guide<_refQuickstart>` contains all the necessary prerequisites, but we'll briefly go through them here again, and include some other details.

***********
Data folder
***********

You must have a data folder where SegnoSharp can access additional binaries, and where it will store its databse if you are running with SQLite.

BASS
====

You must have the BASS libraries from `un4seen <https://www.un4seen.com/bass.html>`_. See the :ref:`BASS section<_refQuickstartBass>` in the :ref:`quickstart guide<_refQuickstart>` for details.

.. note:: The quickstart guides you to download the Linux libraries. If you are going to run SegnoSharp in a Windows environment you will need the ``Win32`` version of these libraries. See the table below for details:

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

What was downloaded from the quickstart guide should be enough, unless you plan on running SegnoSharp on Windows.
If you do then you need to download a Windows distribution of FFMPEG and place ``ffmpeg.exe`` in the ``ffmpeg`` folder inside the ``data`` folder.

***********
Source code
***********

If you want to run this through .NET or Visual Studio you need to clone the source code repository:

::

    git clone https://github.com/whitestone-no/SegnoSharp
