Outliner2-SF
============
This project brings the great but deprecated original Outliner 2.0 (see below) to modern versions of 3ds Max.<br>
The original Outliner 2.0 version has been incompatible with 3ds Max since at least 3ds Max 2017.<br><br>
While i have released several compatibility fixes overtime, removal of the legacy keyboard shortcut system<br>
and the introduction of .NET 8 with 3ds Max 2026 required a more extensive rewrite.<br>

Outliner2-SF is the result of this effort and restores compatibility with modern 3ds Max versions ( up to 2027 ).

License
-------
This project is licensed under the BSD license, see LICENSE.txt  

Installation
------------
-  Download or build the maxscript installer package yourself (see below)
-  install by dropping onto your 3ds Max viewport
-  follow the instructions


Building Requirements
---------------------
* Installed 3ds Max (2016 or newer), default setup uses 3ds Max 2025
* Matching framework/runtime depending on the target version<br>
  .NET Framework 4.x, .NET 8 or .NET 10
* Visual Studio 2022 (recommended) or VS Build Tools 2022 (minimal)
  - MSBuild build tools
  - .NET Framework build tools  
* 7-Zip (for building the installer)

Building
--------
- run provided `buildandbundle.bat` script

If the build environment is correctly configured, running the `buildandbundle.bat` script will:
  - Build the Outliner assembly  (`outliner.dll`) 
  - Create a complete MaxScript installer package (`*.mzp`) ready to be dropped onto the 3ds Max viewport

Notes
-----
The project is configured against a local 3ds Max 2025 installation, the last version using .NET Framework 4.8.1.<br>

ℹ️ The project relies on the installed 3ds Max setup via the corresponding environment variable (see **Outliner.csproj**)<br>

**`ADSK_3DSMAX_x64_2025`**

Depending on the build machine, both the environment variable and the target framework may need to be adjusted.

Using a 3ds Max 2025 setup has proven to be the most flexible, still producing builds compatible with earlier and later 3ds Max versions.

Required Autodesk assemblies (`Autodesk.Max.dll`, `ManagedServices.dll`) are **intentionally** not included in the repository.
