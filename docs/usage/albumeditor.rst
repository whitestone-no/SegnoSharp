.. _usageAlbumEditor:

############
Album Editor
############

The Album Editor is a tool that allows you to create and edit albums, which are collections of discs and tracks.

******
Albums
******

Search
======

The first page you'll see in ``Albums`` is the search page. Here you can search for albums by name.
The results are listed in the order in which they were added to the database, with the most recent additions at the top.
If there are more than 10 search results there is a pager at the bottom to see more results.

Click an album title to edit this album.

Edit Album
==========

This is where you edit the album details.
Use the ``Save`` button at the bottom of the page to save the changes, the ``Close`` button to go back to the album editor,
and the ``Delete`` button to delete the track.


Album data
----------
+-----------------+-----------------------------------------------------------------------------------------------------+
| Title           | The title of the album.                                                                             |
+-----------------+-----------------------------------------------------------------------------------------------------+
| Published       | The year the album was published.                                                                   |
+-----------------+-----------------------------------------------------------------------------------------------------+
| Genres          | | Which genres the album belongs to.                                                                |
|                 | | This is a special field where it searches for existing genres as you type.                        |
|                 | | Select one of the existing genres that show up, or create a new if it doesn't exist.              |
|                 | | See the :ref:`Genres <refAlbumEditorGenres>` section on how to manage genres.                     |
+-----------------+-----------------------------------------------------------------------------------------------------+
| Record labels   | | Which record labels published the album.                                                          |
|                 | | Uses the same field handling as the Genres field.                                                 |
|                 | | See the :ref:`Record Labels <refAlbumEditorRecordLabels>` section on how to manage record labels. |
+-----------------+-----------------------------------------------------------------------------------------------------+
| UPC             | The UPC code (barcode) of the album.                                                                |
+-----------------+-----------------------------------------------------------------------------------------------------+
| Catalgue number | The catalogue number with the publisher for the album.                                              |
+-----------------+-----------------------------------------------------------------------------------------------------+
| Is public       | | If the album should be included in a list displayed for non-authenticated users.                  |
|                 | | Tracks in the :ref:`playlist <refUsagePlaylist>` will be displayed regardless of this setting.    |
+-----------------+-----------------------------------------------------------------------------------------------------+

.. _refAlbumEditorAlbumCredits:

Credits
-------

Credits are composed of a :ref:`credit group <refAlbumEditorCreditGroups>`.
You must add a credit group to an album before you can add people/groups to it.
Select a credit group from the ``Add group`` dropdown list and click the ``Add`` button.
You can now add people/groups to the credit group. This uses the same field handling as the Genres field.

If you create a new entry it will automatically split firstname and lastname. ``John Williams`` becomes ``John`` and ``Williams``.
The split is performed at the the last space. ``James Newton Howard`` becomes ``James Newton`` and ``Howard``.
If you want to have more control you can write the lastname first and separate the lastname from the firstname with a comma.
``Newton Howard, James`` becomes ``James`` and ``Newton Howard``.

See the :ref:`Persons <refAlbumEditorPersons>` section on how to manage people/groups.

Cover
-----

Upload a new cover image, or remove the existing one.
Supports JPEG and PNG smaller than 5MB.

Discs
-----

An album must contain at least one disc, even if this is a digital album.

+-------------+-----------------------------------------------------------------------------------------------+
| Disc number | The number of this disc. Usually ``1``                                                        |
+-------------+-----------------------------------------------------------------------------------------------+
| Disc title  | An optional title for this disc                                                               |
+-------------+-----------------------------------------------------------------------------------------------+
| Media types | | A disc can be of different types, such as CD, Digital Download, SACD, etc.                  |
|             | | Select zero, one or more media types that apply to this disc.                               |
|             | | See the :ref:`Media Types <refAlbumEditorMediaTypes>` section on how to manage media types. |
+-------------+-----------------------------------------------------------------------------------------------+

Remove a disc by clicking the ``Delete disc`` button.

Tracks
------

Each disc should contain at least one track.
Add more tracks by clicking the ``Add track`` button.
You can also reorder the tracks by dragging them up or down. You can also drag tracks between discs.
Click a track title to edit the track details.

Track groups
^^^^^^^^^^^^

A track group is a collection of tracks that are related in some way.
For example, a track group could be a suite of tracks that all come from the same episode of a TV show.
You can add a track group by clicking the ``Add track group`` button.
Give the track group a title and drag it to the correct position in the list.
You can remove a track group by clicking the small ``(X)`` to the right of the title.

Track Editor
^^^^^^^^^^^^

Edit the track details here.
Use the ``Save`` button at the bottom of the page to save the changes, the ``Close`` button to go back to the album editor,
and the ``Delete`` button to delete the track.

Track Data
""""""""""

+---------+----------------------------------------------------------------------------------------+
| Track # | | The number of this track on the disc                                                 |
|         | | Changing this number will automatically reorder the other tracks on the disc.        |
+---------+----------------------------------------------------------------------------------------+
| Title   | The title of the track                                                                 |
+---------+----------------------------------------------------------------------------------------+
| Length  | The running time of the track. Input in ``hours:minutes:seconds``, i.e. ``00:12:34``   |
+---------+----------------------------------------------------------------------------------------+
| Notes   | Any special notes about the track should go here                                       |
+-----------------+--------------------------------------------------------------------------------+

Credits
"""""""

This is where you add people/groups to the track.
See the :ref:`Credits <refAlbumEditorAlbumCredits>` section for albums on how to manage this.

Stream Info
"""""""""""

If this track has a corresponding media file you can add information about this here.
If the stream info doesn't already exist, click the ``Add stream info`` button.

.. _usageAlbumEditorWeights:

+--------------------------+---------------------------------------------------------------------------------------------------------+
| Include in auto playlist | | Should the :ref:`playlist module <refUsagePlaylist>` automatically include this track when adding to  |
|                          | | playlist.                                                                                             |
+--------------------------+---------------------------------------------------------------------------------------------------------+
| File path                | The full path for the media file, including folders and filename.                                       |
+--------------------------+---------------------------------------------------------------------------------------------------------+
| Weight                   | | When the playlist module selects a track it will use this value to determine                          |
|                          | | how often this track should be played. The higher the value, the more often                           |
|                          | | the track will be played, and the lower the value the less it will be played.                         |
|                          | | Default value is 100, so a value of 10 means it has 10 times lower chance                             |
|                          | | of being selected.                                                                                    |
+--------------------------+---------------------------------------------------------------------------------------------------------+

Delete the stream info by clicking the ``Remove`` button.
This will only remove the reference to the media file, not the media file itself.


.. _refAlbumEditorPersons:

*******
Persons
*******

This is where you edit persons/artists/groups that have been added to an album/track.
You cannot create a new entry here as you are meant to add them when creating the album/track.

Search for an existing person/artist/group by typing in the search field.
Matching results will be shown in the list below as you type.

Edit the name of the person/artist/group by editing directly in the results.

You can delete a person/artist/group by clicking the ``(X)`` button to the right of the name.

Variant
=======

If two or more persons/artists/groups have the same name a variant number is automatically added to distinguish between them.
I.e. "John Williams" the composer was added to an album, then later "John Williams" the guitarist is added to another.
John Williams the composer will be "John Williams" without a variant number (though ``0`` will be displated in this table),
and John Williams the guitarist will be "John Williams (1)".

You cannot edit this number directly, but you can change the name of the person/artist/group to remove the variant number.

If a person/artist/group is deleted, variant numbers for other matching persons/artists/groups will be updated to reflect the new order.

``Save`` or ``Discard changes`` by clicking the respective buttons at the bottom of the page.

.. _refAlbumEditorRecordLabels:

*************
Record Labels
*************

This is where you edit record labels that have been added to an album/track.
You cannot create a new entry here as you are meant to add them when creating the album/track.

Search for an existing person/artist/group by typing in the search field.
Matching results will be shown in the list below as you type.

Edit the name of the record label by editing directly in the results.

Any changes you make won't be saved until you click the ``Save`` button at the bottom of the page.

You can delete a record label by clicking the ``(X)`` button to the right of the name.

.. _refAlbumEditorGenres:

******
Genres
******

This page lists all genres that have been added to the database.

Edit the name of the genre by editing directly in the results.

You can delete a genre by clicking the ``(X)`` button to the right of the name.

Add a new genre by clicking the ``Add new genre`` button.

Any changes you make won't be saved until you click the ``Save`` button at the bottom of the page.

.. _refAlbumEditorCreditGroups:

*************
Credit groups
*************

This page lists all credit groups that have been added to the database.
A credit group can be used on either an album or a track, so make sure you create/edit the correct entry if there are multiple
credit groups with the same name.

Create a new group by clicking the ``Add`` button in the correct section.

Delete a group by clicking the ``(X)`` button to the right of the name.

``Include in auto playlist`` is used by the :ref:`playlist module <refUsagePlaylist>` to determine
if this credit group is eligible for inclusion in the automatically generated playlist.

.. _refAlbumEditorMediaTypes:

***********
Media Types
***********

This page behaves exactly like the :ref:`Genres <refAlbumEditorGenres>` page, but for media types.