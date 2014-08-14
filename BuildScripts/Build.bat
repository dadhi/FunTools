@echo off

set SLN="..\FunTools.sln"
set PROJECT_OUTDIR="..\bin\Release"

echo:
echo:Building %SLN% into %PROJECT_OUTDIR% . . .
echo:

rem MSBuild 32-bit operating systems:
rem HKLM\SOFTWARE\Microsoft\MSBuild\ToolsVersions\12.0

for /f "tokens=2*" %%S in ('reg query HKLM\SOFTWARE\Wow6432Node\Microsoft\MSBuild\ToolsVersions\12.0 /v MSBuildToolsPath') do (
if exist "%%T" (

echo:
echo:Using MSBuild from "%%T"
echo:

"%%T\MSBuild.exe" %SLN% /t:Rebuild /p:OutDir=%PROJECT_OUTDIR% /p:Configuration=Release /p:RestorePackages=false /m /p:BuildInParallel=true
))

if not "%1"=="-nopause" pause