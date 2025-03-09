
.. _refUsagePlaylist:

########
Playlist
########

This module handles the organization of the playlist, and allows you to view what's coming up, and what has been played previously.
It has both a public facing interface as well as an administration interface.

******
Public
******

Playlist
========

The main page for the public is the ``Playlist`` page. This shows you which track is currently playing and all the next tracks in the playlist.

History
=======

You can expand the menu and get to the the ``History`` page. By default this shows you what has been played today in descending order so that
the latest history is at the top.
There's also an option to select another date so that you can see what was played yesterday, or a week ago.
If there are more than 10 history items there is a pager at the bottom.

**************
Administration
**************

Playlist
========

This is where you organize the playlist. You can reorder the playlist or remove an item from it.

You can also search for tracks and add them to the playlist, either by clicking the ``Queue top`` or ``Queue bottom`` buttons,
or by dragging the track into the appropriate spot.

The search option should be fairly self-explanatory, except for "Only auto playlist".
Enabling this will only search for albums that are also used by the auto playlist feature.

.. _refUsagePlaylistSettings:

Settings
========

This administration submenu is where you set the options for that auto playlist feature.

+-------------------------+--------------------------------------------------------------------------+
| Minimum number of songs | | The auto playlist will always have a minimum of N number of songs      |
|                         | | queued                                                                 |
+-------------------------+--------------------------------------------------------------------------+
| Minimum total duration  | | The total duration of all songs in the playlist should always be more  |
|                         | | than this value                                                        |
+-------------------------+--------------------------------------------------------------------------+

Both of these are taken into account at the same time, so with the respective default values of ``3`` and ``15``,
if there are 5 tracks in the playlist (more than ``3``), but they only have a total duration of 10 minutes (less than ``15``),
then another track will be added to the playlist.
These rules are checked every 15 seconds.

The settings below are for each playlist processor in the system.
SegnoSharp comes with two processors by default, the ``Default`` processor and the ``Advanced Processor``.

You can enable/disable processors by checking/unchecking the checkbox to the left of the processor name.
The ``Advanced processor`` is disabled by default, but you can turn it on if you choose.

.. note:: The ``Default`` processor cannot be disabled.

If you set too strict rules for a processor so that it isn't able to find any tracks that fulfill the rules,
the next processor in the list will try, and so on until it reaches the ``Default`` processor, which should
always be able to find something.

If there are more than two processors you can reorder the processors, except the ``Default`` processor which will always be the last.

See :ref:`Extending playlist <refExtendingPlaylist>` to see how you can create your own playlist processor.

Weighting
---------

All the processors that comes with SegnoSharp by default has a setting to enable/disable weighted randomization.

This means that, apart from the specific rules, not all tracks have an equal chance at getting selected.

A track with a weight of 10 has a one tenth chance of being selected than a track with a weight of 100.
Similarilly a track with a weight of 300 has three times the chance of being selected than a track with a weight of 100.

See also :ref:`Weight (Album editor) <usageAlbumEditorWeights>`.

