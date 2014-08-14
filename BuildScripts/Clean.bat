@echo off

echo:
echo:Deleting Release and Debug folders..
echo:

pushd ".."

for /d /r %%D IN (b?n;o?j) do (
	if exist "%%D\Release" echo "%%D\Release" & rd /s /q "%%D\Release" 
	if exist "%%D\Debug" echo "%%D\Debug" & rd /s /q "%%D\Debug" 
)

echo:
echo:Cleaning succeded.
echo:

popd

if not "%1"=="-nopause" pause