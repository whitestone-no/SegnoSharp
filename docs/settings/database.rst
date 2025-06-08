.. _refDatabase:

########
Database
########

The database engine used by SegnoSharp by default is `SQLite <https://sqlite.org/>`_.
This is written to the ``data`` directory by default (see :ref:`previous chapter <refConfigurationDatapath>`) and the default database name is ``SegnoSharp.db``.

SegnoSharp also supports `MySQL <https://www.mysql.com/>`_, `Microsoft SQL Server <https://www.microsoft.com/en-us/sql-server/sql-server-downloads>`_, and `PostgreSQL <https://www.postgresql.org/>`_.
You can switch which database is used by the configuration options described below.

.. note:: All environment variables must be prefixed by ``SegnoSharp_``. This prefix is not displayed below for brevity.

.. note:: Configuration options are displayed using dot-notation. For environment variables replace dots with two underscores. For User Secrets either replace dots with colons, or create a corresponding JSON structure.

+-------------------------------+-----------------------------------------------------+--------------------------------------------------------------------------------+
| Database.Type                 | ``sqlite``, ``mysql``, ``mssql``, or ``postgresql`` | Which database engine to use                                                   |
+-------------------------------+-----------------------------------------------------+--------------------------------------------------------------------------------+
| Database.SensitiveDataLogging | ``true`` or ``false``                               | Whether data inserted or queried to the database is logged. Default: ``false`` |
+-------------------------------+-----------------------------------------------------+--------------------------------------------------------------------------------+
| ConnectionStrings.SegnoSharp  | Secret string value                                 | Connection information for selected database engine                            |
+-------------------------------+-----------------------------------------------------+--------------------------------------------------------------------------------+

.. note:: Only set ``SensitiveDataLogging`` to ``true`` when troubleshooting and you need additional information in the logs.

*****************
Database creation
*****************

SegnoSharp uses `Entity Framework Migrations <https://learn.microsoft.com/en-us/ef/core/managing-schemas/migrations/>`_.
This will create the database if it is non-existing, and if there are future changes to the database schema it will also automatically update your database.
Make sure that the database user specified in the connection string has the necessary permissions (create, alter, etc.) so that SegnoSharp can keep its database up-to-date.

*******************************
A special note about PostgreSQL
*******************************

A special note about PostgreSQL:

This database engine is not like other database engines, especially when it comes to value comparisons.
PostgreSQL is case sensitive. You can perform case insensitive searches using a special SQL syntax that no other database engine uses: ``ILIKE``

The other database engines uses ``LIKE`` and case insensitive collations so when there is a search in SegnoSharp it will not differ between ``SegnoSharp`` and ``segnosharp``.
PostgreSQL *will* differ between them, so you have to use the same casing when searching as what you are looking for.