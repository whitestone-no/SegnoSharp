##############
Media Importer
##############

This module allows you to import your existing media files into the SegnoSharp database.
It is a process through four steps in which you select the folder to import, select the files to import, select the metadata to import, and finally import the files.

******
Step 1
******

Here you will select which folder to import. The first time you load this module the ``music`` folder is selected by default.
See :ref:`CommonConfig.MusicPath <refConfigurationDatapath>` for more information on how to change this.

+--------------------+---------------------------------------------------------------------------------+
| Folder             | | You can naviate the folder structure by clicking on the folder names in the   |
|                    | | list, and go to the parent folder by clicking the ``..`` folder.              |
+--------------------+---------------------------------------------------------------------------------+
| Selected path      | | This shows you the currently selected path.                                   |
|                    | | You can click the elements in the path to that folder.                        |
+--------------------+---------------------------------------------------------------------------------+
| Include subfolders | If checked, all subfolders of the selected path will be included in the import. |
+--------------------+---------------------------------------------------------------------------------+

When you have selected the folder you want to import, click the ``Continue`` button to proceed to the next step.

******
Step 2
******

In this step you will select the files you want to import.
This step will only display supported audio formats. See :ref:`FAQ <faqAudioFormats>`.

It will display the path selected in the previous step, as well as whether subfolders are included.
These are not selectable, but you can go back to the previous step to change the selection.

Normalization articles
======================

This is a comma separated list of articles.
Default values in the standard configuration are: ``a``, ``an``, and ``the``.

If an album title starts with one of these articles, it will be moved to the end of the title.
For example, ``The Dark Side of the Moon`` will be normalized to ``Dark Side of the Moon, The``.

Any additional articles you add here will be automatically saved for the next time you import files.

Article normalization will not be performed if the checkbox to the right is not checked.

.. note:: What is an article? See `Wikipedia <https://en.wikipedia.org/wiki/Article_(grammar)>`_.

File selection
==============

This list contains all the files in the folder (and subfolders if included) that are of the supported audio formats.

+--------------------+-----------------------------------------------------------------------------+
| Import file        | The metadata in the file will be imported into the database                 | 
+--------------------+-----------------------------------------------------------------------------+
| Import to playlist | | If this is deselected then the file will *not* be imported as a file that |
|                    | | can be played! Only the metadata will be imported.                        |
+--------------------+-----------------------------------------------------------------------------+

When you have selected the files you want to import, click the ``Continue`` button to proceed to the next step.

******
Step 3
******

Here you will see all metadata read from the selected files, and you can adjust any information you want to change.
This basically follows the same structure as the :ref:`Album editor <usageAlbumEditor>` module.

.. note:: If you have selected many files, this step may take some time to load. Do not navigate away from this page as long as ``Loading. Please wait`` is displayed.

Album
=====

If an album with the same title already exists in the database, any files/tracks you are about to import will be added to that album.
If you want to create a new album, click the ``Create a copy`` button. The text `` - Copy N`` will be added to the album title, where N is the next available number.

+-----------------+-----------------------------------------------------------------------------------------------------+
| Title           | The title of the album.                                                                             |
+-----------------+-----------------------------------------------------------------------------------------------------+
| Published       | The year the album was published.                                                                   |
+-----------------+-----------------------------------------------------------------------------------------------------+
| Genres          | | A comma separated list of genres.                                                                 |
|                 | | If a genre does not exist in the database it will be created.                                     |
+-----------------+-----------------------------------------------------------------------------------------------------+
| Album artist(s) | | A comma separated list of artists.                                                                |
|                 | | If an artist does not exist in the database they will be created.                                 |
|                 | | Artist names will be automatically normalized into ``Firstname`` and ``Lastname``.                |
|                 | | ``John Doe`` will be normalized to ``John`` and ``Doe``, and                                      |
|                 | | ``John William Doe`` will be normalized to ``John`` and ``William Doe``.                          |
|                 | | Verify the albums in the Album editor after the import to ensure the normalization                |
|                 | | is correct, and adjust the names in :ref:`Person editor <refAlbumEditorPersons>` if necessary.    |
+-----------------+-----------------------------------------------------------------------------------------------------+
| Is public       | | If the album should be included in a list displayed for non-authenticated users.                  |
|                 | | Tracks in the :ref:`playlist <refUsagePlaylist>` will be displayed regardless of this setting.    |
+-----------------+-----------------------------------------------------------------------------------------------------+

Cover
=====

If any of the files you selected comes with an embedded cover image, it will be displayed here.
You can upload a new cover image, or remove the existing one.
Supports JPEG and PNG smaller than 5MB.

Disc
====

An album will automatically contain at least one disc.

+-------------+-----------------------------------------------------------------------------------------------+
| Disc number | The number of this disc. Usually ``1``                                                        |
+-------------+-----------------------------------------------------------------------------------------------+
| Disc title  | An optional title for this disc                                                               |
+-------------+-----------------------------------------------------------------------------------------------+
| Media types | | A disc can be of different types, such as CD, Digital Download, SACD, etc.                  |
|             | | Select an optional media types that apply to this disc.                                     |
|             | | See the :ref:`Media Types <refAlbumEditorMediaTypes>` section on how to manage media types. |
+-------------+-----------------------------------------------------------------------------------------------+

Tracks
======

Each file you selected in the previous step will be imported as a track. Each track has multiple fields that will be activated when you click on them.

This list can be reordered by dragging the tracks up or down, or by manually changing the ``Track #`` field.

+-------------+-----------------------------------------------------------------------------------------------------------+
| Length      | | Automatically filled in when the file is read, but you can change it if it is incorrect.                |
|             | | Use the ``hours:minutes:seconds`` format.                                                               |
+-------------+-----------------------------------------------------------------------------------------------------------+
| Autoplay    | Defines whether the track is eligible for automatic enlistment in the :ref:`playlist <refUsagePlaylist>`. |
+-------------+-----------------------------------------------------------------------------------------------------------+
| Title       | The title of the track.                                                                                   |
+-------------+-----------------------------------------------------------------------------------------------------------+
| Artist(s)   | A comma separated list of artists. See ``Album artist(s)`` for more information.                          |
+-------------+-----------------------------------------------------------------------------------------------------------+
| Composer(s) | A comma separated list of composers. See ``Album artist(s)`` for more information.                        |
+-------------+-----------------------------------------------------------------------------------------------------------+

All done
========

When you have adjusted all the metadata to your liking, click the ``Continue`` button .

******
Step 4
******

In this final step you will see a brief summary of what will be imported: Number of albums and number of tracks.

If you are happy with this click the ``Finish`` button to start the import process.

The message displayed will change to ``Import in progress. Please wait.`` and the import process will start.
Do not navigate away from this page until it is complete.
If you selected many files it may take some time to import them all.

When the import is finished you will see a ``Import complete!`` message.