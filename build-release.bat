@echo off
pushd "%~dp0"
dotnet publish -c Release -r win-x64 /p:PublishSingleFile=true /p:PublishTrimmed=true
copy bin\Release\net8.0\win-x64\publish\ToPdf.exe ..\..\dwf981@bb\Main\Developer\buildsystem\bin\
popd
