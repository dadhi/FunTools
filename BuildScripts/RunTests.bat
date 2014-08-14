@echo off

echo:
echo:Running tests..
echo:

pushd ".."

set NUNIT="packages\NUnit.Runners\tools\nunit-console.exe"

for %%D in ("bin\Release\*Tests*.dll") do %NUNIT% %%D

popd

echo:
echo:Tests succeeded.
echo:

if not "%1"=="-nopause" pause