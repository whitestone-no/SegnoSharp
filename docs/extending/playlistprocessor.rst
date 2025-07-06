.. _refExtendingPlaylist:

###################
Playlist processors
###################

You can add your own playlist processors.
First you must create a new module, so first see :ref:`Modules <refExtendingModules>` on how to create a module.

******
Basics
******

A playlist processor class must inherit from ``Whitestone.SegnoSharp.Shared.Interfaces.IPlaylistProcessor``.
This class must be registered with the dependency injection container for SegnoSharp to find it.

This is done in the ``ConfigureServices()`` in the main class for your module:

::

    public void ConfigureServices(IServiceCollection services, IHostEnvironment environment, IConfiguration configuration)
    {
        // See note below for why this must be a singleton
        services.AddSingleton<IPlaylistProcessor, MyPlaylistProcessor>();
    }

******************
Processor settings
******************

The ``IPlaylistProcessor`` interface has a ``Settings`` property. You can create a new class that inherits from ``Whitestone.SegnoSharp.Shared.Models.PlaylistProcessorSettings`` and implement it like this:

::

    public PlaylistProcessorSettings Settings { get; set; } = new MyPlaylistProcessorSettings();

This class will be automatically register with SegnoSharp so that any changes to properties in your settings class will automatically be written to the database.
Similarilly their values will be read from the database at startup. This way any modified settings will be kept from run to run.

.. note:: The playlist processor must be added to dependency injection as a Singleton. This is so that there will only be one instance of the settings.

********************
Playlist integration
********************

When the SegnoSharp needs to add a track to the playlist (see :ref:`playlist settings <refUsagePlaylistSettings>`) it will call the ``GetNextTrackAsync()`` method
on the registered playlist processors until it gets a track.

Your implementation of this method should check its rules and return a ``TrackStreamInfo`` from the database that matches the rules.
If it doesn't find a ``TrackStreamInfo`` that matches the rules it should return ``null``. This will make SegnoSharp go to the next playlist processor
in the list and run ``GetNextTrackAsync()`` on that. This will continue until someone returns a track.