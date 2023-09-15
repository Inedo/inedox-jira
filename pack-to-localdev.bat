@echo off

dotnet new tool-manifest --force
dotnet tool install inedo.extensionpackager

cd Jira\InedoExtension
dotnet inedoxpack pack . C:\LocalDev\BuildMaster\Extensions\Jira.upack --build=Debug -o
cd ..\..