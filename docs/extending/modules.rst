.. _refExtendingModules:

#######
Modules
#######

SegnoSharp is written to be extendible, so you can create your own modules.
This is not for the faint hearted and technically illiterate.

The easiest way to get started with a new module is by cloning the repository and making sure it runs properly.
See how to run the application in either :ref:`Visual Studio <refRunningVisualStudio>` or :ref:`VS Code <refRunningVsCode>`.


******
Basics
******

Once you have cloned the repository, create a new folder in ``src/Modules`` with the name of your module.

You will now need a project file for this module. The easiest is to copy on of the project files from an existing module and rename it to ``<yourModuleName>.csproj``.
The existing modules should have all the settings you need to get started, and it will also be automatically copied to the correct output folder so that SegnoSharp
will be able to find it: ``src/SegnoSharp/bin/Debug/net9.0/Modules/YourModuleName``.
You should consider changing the ``Product / Company / Authors`` to suit yourself, as well as the ``RootNamespace``.

.. note:: The compiled DLL for the module *must* start with ``Module.``, otherwise SegnoSharp will not find it.

Inside your new module folder, create a class called ``Module.cs``.

.. note:: This class *must* inherit from ``Whitestone.SegnoSharp.Shared.Interfaces.IModule``, otherwise SegnoSharp will not find it.

In order to properly inherit this interface you must implement the following property and method:

::

    public Guid Id { get; } = Guid.NewGuid();

    public void ConfigureServices(IServiceCollection services, IHostEnvironment environment, IConfiguration configuration)
    {
    }

The ``Id`` is not specifically used anywhere important, but it should be unique across the various modules. ``.NewGuid()`` is enough in most cases.
It is important that the same ``Guid`` is returned every time, so use the syntax above and do not use ``public Guid Id => Guid.NewGuid();``
as this will return a different ``Guid`` every time the property is accessed.

Inside ``ConfigureServices()`` you add any classes you need into the dependency injection container.
You can i.e. read configuration parameters and register them using the `Options Pattern <https://learn.microsoft.com/en-us/aspnet/core/fundamentals/configuration/options>`_,
or you can add long running services using ``IHostedService``. See `Microsoft <https://learn.microsoft.com/en-us/dotnet/core/extensions/timer-service>`_'s documentation for more details.

***********
Razor pages
***********

You can also add Razor pages. These should be places in a ``Components/Pages`` folder inside your project.
Make sure that any ``@page`` directives you use are not already being used by another module. If in doubt, just start SegnoSharp and navigate to the page.
If there are multiple pages with the same directive you will get an exception.

You can use ``@rendermode InteractiveServer`` if you want to have your module use Blazor, otherwise it will be loaded as a regular static web page.

.. note:: If you use the ``@rendermode`` directive you should also add an ``_Imports.razor`` file to your ``Components`` folder
    that contains ``@using static Microsoft.AspNetCore.Components.Web.RenderMode``

****************
Menu integration
****************

Decorate your Razor pages with the ``ModuleMenu`` attribute to have this page shown in the main navigation menu in SegnoSharp.
Here are some examples on how to use ``ModuleMenu``:

.. note:: SegonSharp uses icons from `Font Awesome <https://fontawesome.com/>`_. See their webpage for a list of available (free) icons.

::

    // A main menu title called "MyPage" will be added to the public section of the menu
    @attribute [ModuleMenu("MyPage", false)]

    // A main menu title called "MyPage" will be added to the admin section of the menu
    @attribute [ModuleMenu("MyPage", true)]

    // A main menu title called "MyPage" will be added to the admin section of the menu with the "list" icon.
    @attribute [ModuleMenu("MyPage", "fa-list", true)]

    // A main menu title called "MyPage" will be added to the admin section of the menu with a sort order of 50.
    // This means it will be placed below menu items with a lower sort order, and above menu items with a higher
    // sort order. Default sort order if not specified is 100.
    @attribute [ModuleMenu("MyPage", 50, true)]

    // A sub menu title called "MySubPage" will be added as a child of "MyPage" to the public section of the menu
    @attribute [ModuleMenu("MySubPage", typeof(MyPage), false)]

***************
Database access
***************

The ``.csproj`` file includes a reference to the ``Database`` project, so in order to use the database you should inject ``IDbContextFactory<SegnoSharpDbContext>``
into your constructor.
From this you can create an Entity Framework database context using ``await dbContextFactory.CreateDbContextAsync()``.

.. note:: If you create your ``DbContext`` as a global class variable instead of just creating it on demand, you must remember to dispose it.

