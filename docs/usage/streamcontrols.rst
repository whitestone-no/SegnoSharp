######
Stream
######

This is the main control center for the Shoutcast/Icecast connection.
Let's start from the bottom:

**********************
Stream server settings
**********************

These are the settings that determine which Shoutcast/Icecast server to stream to, and some meta information for the server.

+----------------+----------------------------------------------------------------------------------------+
| Audio format   | | The compressed audio format to send to the Shoutcast/Icecast server.                 |
|                | | One of either ``MP3`` or ``AAC``                                                     |
+----------------+----------------------------------------------------------------------------------------+
| Bitrate        | | How much to compress the audio sent to the server.                                   |
|                | | The higher the number, the higher the quality, but also more network bandwidth used. |
+----------------+----------------------------------------------------------------------------------------+
| Server type    | Stream to Shoutcast or Icecast                                                         |
+----------------+----------------------------------------------------------------------------------------+
| Hostname       | The hostname of the Shoutcast/Icecast server                                           |
+----------------+----------------------------------------------------------------------------------------+
| Port           | The TCP port number of the Shoutcast/Icecast server                                    |
+----------------+----------------------------------------------------------------------------------------+
| Mount point    | | The Stream ID (Shoutcast) or path (Icecast) to the mount point                       |
|                | | on the Shoutcast/Icecast server                                                      |
+----------------+----------------------------------------------------------------------------------------+
| Password       | Streaming to a Shoutcast/Icecast server requires a password. Put this here.            |
+----------------+----------------------------------------------------------------------------------------+
| Admin password | Shoutcast/Icecast admin password for retrieval of statistics.                          |
+----------------+----------------------------------------------------------------------------------------+
| Is public      | | Meta data for the Shoutcast/Icecast server.                                          |
|                | | Should this stream be shown in a public directory on the Shoutcast/Icecast server    |
|                | | or not                                                                               |
+----------------+----------------------------------------------------------------------------------------+
| Name           | | Meta data for the Shoutcast/Icecast server.                                          |
|                | | The name of the stream as shown on the server.                                       |
+----------------+----------------------------------------------------------------------------------------+
| Server URL     | | Meta data for the Shoutcast/Icecast server.                                          |
|                | | A custom URI that the server will link to. Does not have to link back to SegnoSharp. |
+----------------+----------------------------------------------------------------------------------------+
| Genre          | | Meta data for the Shoutcast/Icecast server.                                          |
|                | | The genre of the stream as shown on the server.                                      |
+----------------+----------------------------------------------------------------------------------------+
| Description    | | Meta data for the Shoutcast/Icecast server.                                          |
|                | | The description of the stream as shown on the server.                                |
+----------------+----------------------------------------------------------------------------------------+

***************
Stream controls
***************

This controls the actual audio stream sent to Shoutcast/Icecast.

+-----------------+------------------------------------------------------------------------------------------+
| Play next track | | Convenience function to stop the currently playing track and immediately play          |
|                 | | the next track in the playlist.                                                        |
+-----------------+------------------------------------------------------------------------------------------+
| Start streaming | | Connect to Shoutcast/Icecast using the configuration in ``Stream server settings``,    |
|                 | | start encoding the audio stream, and start sending it to the server.                   |
+-----------------+------------------------------------------------------------------------------------------+
| Stop streaming  | Disconnect from Shoutcast/Icecast and stop encoding.                                     |
+-----------------+------------------------------------------------------------------------------------------+
| Volume          | The audio volume of the audio stream.                                                    |
+-----------------+------------------------------------------------------------------------------------------+
| Title           | | When a track starts playing SegnoSharp will send meta data information about this      |
|                 | | specific track to the Shoutcast/Icecast server. You can modify this meta data here     |
|                 | | Use combinations of ``%album%``, ``%title%``, and/or ``%artist%``, or add some         |
|                 | | static text if you want. See note below for more details.                              |
+-----------------+------------------------------------------------------------------------------------------+
| Connect         | | If this option is turned on SegnoSharp will connect, start encoding, and start sending |
|                 | | to Shoutcast/Icecast when SegnoSharp starts. This is convenient so that if             |
|                 | | SegnoSharp needs to restart for a reason, then you don't have to log back on to        |
|                 | | manually start streaming.                                                              |
+-----------------+------------------------------------------------------------------------------------------+

.. note:: The ``%artist%`` token will first check if there are any credit groups on the track that are marked as
    ``Include in auto playlist`` that also has one or more artist.
    If there are then ``%artist`` will use the information from the track.
    If the track doesn't contain any credits, then it will do the same check for album credits.    
    See also :ref:`Credit Groups (Album Editor) <refAlbumEditorCreditGroups>`