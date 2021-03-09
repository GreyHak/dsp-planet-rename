@ECHO OFF
"%ProgramFiles%\7-Zip\7z" a ..\GreyHak-DSP_Planet_Rename-9.zip icon.png LICENSE.txt manifest.json README.md
cd bin\Release
"%ProgramFiles%\7-Zip\7z" u ..\..\..\GreyHak-DSP_Planet_Rename-9.zip DSPPlanetRename.dll
