#############
Visual Studio
#############

.. note:: This guide is about the regular Visual Studio, *not^ Visual Studio Code

Now that you have the source code you should first make sure that the ``data`` folder in the repository contains the correct files

You can now open ``src/SegnoSharp.sln`` in Visual Studio.

Right-click the ``SegnoSharp`` project and select ``Manage User Secrets``. The ``secrets.json`` that should now be opened should contain the following:

::

    {
      "OpenIdConnect:UseOidc": false
	}

The paths to ``data`` and ``music`` as mentioned in the quickstart is not necessary to specify in Visual Studio. These are already specified in ``SegnoSharp/Properties/launchSettings.json``.

You should now be able to start SegnoSharp by clicking the play-button (or pressing ``F5`` if you're using the default keyboard mappings).

.. note:: Make sure that the play button says "SegnoSharp". If it does not, select it from the dropdown list on the play button.