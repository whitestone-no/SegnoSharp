####
.NET
####

Go to ``src`` in your cloned repository.

Build the solution:

::

    dotnet build .\SegnoSharp.sln
	
And start SegnoSharp with some required arguments:

::

    dotnet run --project .\SegnoSharp\SegnoSharp.csproj --no-launch-profile CommonConfig:DataPath=../../data CommonConfig:MusicPath=../../music OpenIdConnect:UseOidc=false
	
The ``DataPath`` and ``MusicPaths`` are relative to the ``src/SegnoSharp`` folder.
If you do not want to use the ``data`` and ``music`` paths that comes with the repository replace these settings.
Remember that the paths can be both relative and absolute.

Seeing as you have cloned the repository there's nothing stopping you from editing ``src/SegnoSharp/appsettings.json`` instead of having to specifically
set the configuration parameters in the ``dotnet`` command.

.. note:: The repository comes with various launch profiles, but these are not compatible with the Dotnet CLI, so remember to always use the ``--no-launch-profile`` option.