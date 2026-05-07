Outliner2-SF
============
This project brings the great but deprecated original Outliner 2.0 (see below) to modern versions of 3ds Max.
The original plugin has been incompatible with 3ds Max since at least 3ds Max 2017. 
While i have released several compatibility fixes were released over time, the removal of the legacy keyboard
shortcut system and the introduction of .NET 8 with 3ds Max 2026 required a more extensive rewrite.

Outliner2-SF restores compatibility with modern 3ds Max versions, currently supporting 3ds Max 2017–2027(+).

Building Requirements
---------------------
* Installed 3ds Max (2016 or newer)
* Matching framework/runtime depending on the target version:
  .NET Framework 4.x, .NET 8 or .NET 10
* 7-Zip (for building the installer)

Building Notes: 
---------------
The project is currently configured against a local 3ds Max 2025 installation, the last version using .NET Framework 4.8.1. 
Outliner.csproj makes use of the installation via the according environment variable: ADSK_3DSMAX_x64_2025

Depending on the build machine, the environment variable and target framework may need adjustment.
Using the 3ds Max 2025 setup proved to be the most flexible while still producing builds compatible with earlier and later 3ds Max versions.
Required Autodesk assemblies (Autodesk.Max.dll, ManagedServices.dll) are intentionally not included in the repository.

If the build environment is correctly configured, the buildandbundle.bat batch script builds the outliner assembly and creates a maxscript
installer package (*.mzp) of Outliner2-SF



Original README Text below
--------------------------

Outliner
========
The Outliner 2.0 is a fast and easy to use scene management tool. It has a wide  
range of features, including selecting, hiding, freezing, linking and grouping  
objects in the "Hierarchy Mode". In the "Layer Mode" you can organize your scene  
by dragging & dropping objects from one layer to the other.  
What's more, the Outliner offers support for nested layers, to organize scenes  
more efficiently. The interface is nimble enough to keep it open constantly.  

License
-------
This project is licensed under the BSD license, see LICENSE.txt  
  

Requirements
------------
* 3dsmax 2010
* .NET Framework v3.5
* 7zip (to build an installer from the source)


Building
--------
1. Obtain a copy of the source. You can do this by using the git version control  
   system (http://git-scm.com/) to pull the project in.  
   If you're unsure how this works, you can download a .zip from one of these sources:  
   https://github.com/Pjanssen/Outliner/zipball/master (Master/Release branch)  
   https://github.com/Pjanssen/Outliner/zipball/develop (Development/Beta branch)  
   https://github.com/Pjanssen/Outliner/zipball/experiment (Experimental branch)  
   
2. Make sure you have the .NET Framework 3.5 or above. Since 3dsmax has the  
   same requirement, you should be fine here.  
   http://www.microsoft.com/net  

3. Download and install 7-zip  
   http://www.7-zip.org/  

4. Run buildandbundle.bat  
   This will compile the .NET code and create a .mzp installer.  

5. Drag & drop the created file outliner.mzp into 3dsmax to start the installer.  