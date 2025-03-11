#####################
App Inspector
#####################

This is a module to show technical information about SegnoSharp.
Useful for troubleshooting.

It will show the following:

+-----------------------+----------------------------------------------------------------------------+
| Environment           | Information about the operating system and runtime architecture            |
+-----------------------+----------------------------------------------------------------------------+
| Core                  | | Lists the version numbers for the core functionality assemblies:         |
|                       | | SegnoSharp, Shared, Database                                             |
+-----------------------+----------------------------------------------------------------------------+
| Modules               | | Lists all the SegnoSharp modules that have been found and loaded         |
|                       | | as well as their respective versions.                                    |
+-----------------------+----------------------------------------------------------------------------+
| Configuration         | | All the configuration values that have been read by SegnoSharp,          |
|                       | | their configuration keys, their value, and provider (where they          |
|                       | | originate). This includes values from ``appsettings.json``,              |
|                       | | user secrets, and mapped environment variables.                          |
+-----------------------+----------------------------------------------------------------------------+
| Dependency Injection  | | This displays all classes that have been registered with the             |
|                       | | dependency injection container. It contains how the class is registered  |
|                       | | (Lifetime), which interface/class is it registered as (service), and     |
|                       | | which class is registered as the implementation.                         |
+-----------------------+----------------------------------------------------------------------------+
| Environment variables | | All the environment variables available to SegnoSharp. This will include |
|                       | | many of the same settings from ``Configuration``, but also others.       |
+-----------------------+----------------------------------------------------------------------------+