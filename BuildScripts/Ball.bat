@echo off
setlocal

echo:
echo:Clean, build, pack nuget packages..
echo:-----------------------------------

set ScriptsDir="%~dp0"
echo:Switching to %ScriptsDir%
cd /d %ScriptsDir%  

call Clean -nopause
call :Check

call Build -nopause
call :Check

call RunTests -nopause
call :Check

rem call NuGetPack -nopause
rem call :Check

echo:------------
echo:All Success.

if not "%1"=="-nopause" pause 
goto:eof

:Check
if ERRORLEVEL 1 (
echo:Step failed with ERRORLEVEL==%ERRORLEVEL%!
exit
) else ( 
exit /b
)