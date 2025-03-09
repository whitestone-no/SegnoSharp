.. _refRunningVsCode:

##################
Visual Studio Code
##################

.. note:: This guide is about Visual Studio Code (VSCode), *not* the regular Visual Studio

Now that you have the source code you should first make sure that the ``data`` folder in the repository contains the correct files

You should also make sure that you have the following extensions installed in VSCode:

* C#
* C# Dev Kit

Now open the root of the repository in VSCode.

Go to "Run and Debug" from the Primary Side Bar.

This should prompt you to create a ``launch.json`` file. Accept the creation of this file and make sure it has the following content:

::

    {
        "version": "0.2.0",
        "configurations": [
            {
                "name": "C#: SegnoSharp Debug",
                "type": "coreclr",
                "request": "launch",
                "program": "${workspaceFolder}/src/SegnoSharp/bin/Debug/net9.0/Whitestone.SegnoSharp.dll",
                "cwd": "${workspaceFolder}/src/SegnoSharp",
                "env": {
                    "SegnoSharp_CommonConfig__DataPath": "${workspaceFolder}/data",
                    "SegnoSharp_CommonConfig__MusicPath": "${workspaceFolder}/music",
                    "SegnoSharp_OpenIdConnect__UseOidc": "false"
                }
            }
        ]
    }


You can now open ``src/SegnoSharp.sln`` in Visual Studio.

When the repository has been opened, make sure to create a ``tasks.json`` in the same place that ``launch.json`` was created (a hidden folder called ``.vscode``).
Make sure it has the following content:

::

    {
        "version": "2.0.0",
        "tasks": [
            {
                "type": "dotnet",
                "task": "build",
                "file": "${workspaceFolder}/src/SegnoSharp.sln",
                "group": "build",
                "problemMatcher": [],
                "label": "dotnet: Build SegnoSharp solution"
            }
        ]
    }

You can now open the build tasks (default shortcut is ``Ctrl+Shift+B``) and select ``dotnet: Build SegnoSharp Solution``.
This should build the solution and you can observe the build progress in the Terminal.

Now that the solution has been built, go back to "Run and Debug" from the Primary Side Bar, and press the play button, which should have ``C#: SegnoSharp Debug`` next to it.
SegnoSharp will now start.