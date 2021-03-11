# Planet Rename for Dyson Sphere Program

**DSP Planet Rename** is a mod for the Unity game Dyson Sphere Program developed by Youthcat Studio and published by Gamera Game.  The game is available on [here](https://store.steampowered.com/app/1366540/Dyson_Sphere_Program/).

With this mod you can rename planets.  Planets are renamed from the planet or starmap views by clicking on the planet name above the planet details on the right.

If you like this mod, please click the thumbs up at the [top of the page](https://dsp.thunderstore.io/package/GreyHak/DSP_Planet_Rename/) (next to the Total rating).  That would be a nice thank you for me, and help other people to find a mod you enjoy.

If you have issues with this mod, please report them on [GitHub](https://github.com/GreyHak/dsp-planet-rename/issues).  I try to respond within 12 hours.    You can also contact me at GreyHak#2995 on the [DSP Modding](https://discord.gg/XxhyTNte) Discord #tech-support channel..

If you want to rename stars, please see [kassent](https://dsp.thunderstore.io/package/kassent/)'s [StarmapExtension](https://dsp.thunderstore.io/package/kassent/StarmapExtension/) mod.

## Config File
The config file is used to store the planet names.  This will ensure you are able to disable this mod and without having issues with your save file.

The configuration file is called `greyhak.dysonsphereprogram.planetrename.cfg`.  It is generated the first time you run the game with this mod installed.  On Windows 10 it is located at
 - If you installed manually:  `%PROGRAMFILES(X86)%\Steam\steamapps\common\Dyson Sphere Program\BepInEx\config\greyhak.dysonsphereprogram.planetrename.cfg`
 - If you installed with r2modman:  `C:\Users\<username>\AppData\Roaming\r2modmanPlus-local\DysonSphereProgram\profiles\Default\BepInEx\config\greyhak.dysonsphereprogram.planetrename.cfg`

## Installation
This mod uses the BepInEx mod plugin framework.  So BepInEx must be installed to use this mod.  Find details for installing BepInEx [in their user guide](https://bepinex.github.io/bepinex_docs/master/articles/user_guide/installation/index.html#installing-bepinex-1).  This mod was tested with BepInEx x64 5.4.5.0 and Dyson Sphere Program 0.6.16.5831 on Windows 10.

To manually install this mod, add the `DSPPlanetRename.dll` to your `%PROGRAMFILES(X86)%\Steam\steamapps\common\Dyson Sphere Program\BepInEx\plugins\` folder.

This mod can also be installed using ebkr's [r2modman](https://dsp.thunderstore.io/package/ebkr/r2modman/) mod manager by clicking "Install with Mod Manager" on the [DSP Modding](https://dsp.thunderstore.io/package/GreyHak/DSP_Planet_Rename/) site.

## Open Source
The source code for this mod is available for download, review and forking on GitHub [here](https://github.com/GreyHak/dsp-planet-rename) under the BSD 3 clause license.

## Change Log
### v1.0.2
 - Added compatibility with Touhma GalacticScale mod v1.3.0[BETA].
 - Fixed the delayed update of the planet name in the globe planet view.
 - Major speed optimization: planet names now only save to disk when the game saves (still not to the game file).
### v1.0.1
 - Fixed a problem when using the planet view.
 - Fixed a problem creating new galaxies with this mod enabled.
### v1.0.0
 - Initial release.
