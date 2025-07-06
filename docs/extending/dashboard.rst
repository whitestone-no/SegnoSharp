.. _refExtendingDashboard:

###############
Dashboard boxes
###############

You can add additional boxes to the dashboard.
As with :ref:`Playlist processors <refExtendingPlaylist>` you must first create a new module (see :ref:`Modules <refExtendingModules>`).

******
Basics
******

A dashboard box is basically just a Blazor component that inherits from ``Whitestone.SegnoSharp.Shared.Interfaces.IDashboardBox``.
The dashboard box will be loaded dynamically using the ``DynamicComponent`` component in Blazor, so you do not have to register it
in the dependency injection container, but the containing module must.

Your box component must at least implement the ``Name`` property.
This value is used in the admin dashboard settings to identify the box.

The following is a minimal implementation of a dashboard box:

**Example.razor**

::

    <div>Hello dashboard!</div>

**Example.razor.cs**

::

    public class DemoDashboardBox : IDashboardBox
    {
        public static string Name => "Demo Dashboard Box";
    }

After installing the containing module the box will now be available in the admin dashboard settings.

*******************
Additional settings
*******************

The ``IDashboardBox`` interface also provides two optional properties.

* ``Title`` will be displayed in the actual dashboard view and is what users will see as the title of the box.
* ``AdditionalCSS``: If your box needs additional CSS, you can provide a relative path to a CSS file that will be loaded when the box is displayed.
  If two dashboard boxes provide the same CSS file, it will only be loaded once.

***********
Extra notes
***********

As each box is rendered as a ``DynamicComponent`` which also allows for dependency injection, so use ``@inject`` to inject services into your box.

SegnoSharp uses per page/component interactivity, so you can use ``@rendermode InteractiveServer`` to include interactivity in your box.

You can also use ``<AuthorizeView>`` in your boxes to restrict access to your box, or to make parts of your box restricted.