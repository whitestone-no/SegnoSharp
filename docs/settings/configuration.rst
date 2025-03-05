.. _refConfiguration:
#############
Configuration
#############

SegnoSharp has several runtime configuration options.
As SegnoSharp is designed to primarily be used as a Docker Container these configuration options are usually set as environment variables.
Environments supporting Docker containers can usually also use secrets, which are recommended for sensitive configuration options like :ref:`Authentication<refAuthentication>`.
Refer to the documentation for your environment to learn how to use secrets.

If you are cloning the SegnoSharp repository, compiling and running it locally then you should use `User Secrets <https://learn.microsoft.com/en-us/aspnet/core/security/app-secrets>`_.

.. note:: All environment variables must be prefixed by ``SegnoSharp_``. This prefix is not displayed below for brevity.

.. note:: Configuration options are displayed using dot-notation. For environment variables replace dots with two underscores. For User Secrets either replace dots with colons, or create a corresponding JSON structure.

Configuration of :ref:`database<refDatabase>` and :ref:`authentication<refAuthentication>` are covered in their respective chapters.

.. _refConfigurationDatapath:
********************
Common configuration
********************

+---------------------------+------------------------------------------------+------------------------------------------------------------------------------+
| CommonConfig.DataPath     | Absolute path or relative to working directory | Directory used for additional persistent data handling, i.e. SQLite database |
+---------------------------+------------------------------------------------+------------------------------------------------------------------------------+
| CommonConfig.MusicPath    | Absolute path or relative to working directory | This is the default directory SegnoSharp will read music from                |
+---------------------------+------------------------------------------------+------------------------------------------------------------------------------+
| CommonConfig.SharedSecret | Secret string value                            | Used by ``IHashingUtil`` to generate hashes unique to your installation      |
+---------------------------+------------------------------------------------+------------------------------------------------------------------------------+
| Modules.ModulesFolder     | Absolute path or relative to working directory | Change this if you want to load modules from another folder.                 |
+---------------------------+------------------------------------------------+------------------------------------------------------------------------------+

****
BASS
****

SegnoSharp uses the BASS audio libraries from `un4seen <https://www.un4seen.com/bass.html>`_ (see the corresponding chapter in :ref:`quickstart<refQuickstartBass>`).
These libraries are not free for all use, so check if you need a license registration.
This registration can be placed in the following configuration options:

+---------------------------+---------------------+------------------------------+
| BASS.Registration.Key     | Secret string value | Your BASS registration key   |
+---------------------------+---------------------+------------------------------+
| BASS.Registration.Email   | Secret string value | Your BASS registration email |
+---------------------------+---------------------+------------------------------+


*******
Logging
*******

SegnoSharp logs to console by default, using Serilog. The Serilog configuration can be overridden by setting environment variables, or changing the ``appsettings.json`` in the main application if running it from a cloned repo.
In the following example the `Serilog.Sinks.File <https://github.com/serilog/serilog-sinks-file>`_ is added to the logging configuration:

::

    SegnoSharp_Serilog__Using__1=Serilog.Sinks.File
    SegnoSharp_Serilog__WriteTo__1__Name=File
    SegnoSharp_Serilog__WriteTo__1__Args__path=/var/segnosharp/logs/log.txt

You can also overwrite the existing console logger by changing the ``1`` to ``0`` in the above example.

For more settings to override, see the documentation for `Serilog.Settings.Configuration <https://github.com/serilog/serilog-settings-configuration>`_.