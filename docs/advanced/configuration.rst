#############
Configuration
#############


*******
Logging
*******

SegnoSharp logs to console by default, using Serilog. The Serilog configuration can be overridden by setting environment variables.
In the following example the `Serilog.Sinks.File<https://github.com/serilog/serilog-sinks-file>`_ is added to the logging configuration:

::

    SegnoSharp_Serilog__Using__1=Serilog.Sinks.File
    SegnoSharp_Serilog__WriteTo__1__Name=File
    SegnoSharp_Serilog__WriteTo__1__Args__path=/var/segnosharp/logs/log.txt

You can also overwrite the existing console logger by changing the ``1`` to ``0`` in the above example.

For more settings to override, see the documentation for `Serilog.Settings.Configuration<https://github.com/serilog/serilog-settings-configuration>`_.