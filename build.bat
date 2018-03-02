@echo off

echo Locating MSBuild

set libspath=%cd%\Libs
set buildconf=Release
set buildplat=AnyCPU
set vswhere="%ProgramFiles(x86)%\Microsoft Visual Studio\Installer\vswhere.exe"

for /f "usebackq tokens=*" %%i in (`%vswhere% -latest -products * -requires Microsoft.Component.MSBuild -property installationPath`) do (
	set InstallDir=%%i
)

set msbuild="%InstallDir%\MSBuild\15.0\Bin\MSBuild.exe"

if not exist %msbuild% (
	echo Failed to locate MSBuild.exe
	echo This project uses MSBuild 15 to compile
	pause
	exit /b 1
)

if not -%1-==-- (
	echo Using %1 as building configuration
	set buildconf=%1
)
if -%1-==-- (
	echo No custom build configuration specified. Using Release
)

if not -%2-==-- (
	echo Using %2 as building platform
	set buildplat=%2
)
if -%2-==-- (
	echo No custom platform specified. Using AnyCPU
)

rmdir /Q /S "%cd%\ZeroRpc.Net\bin\%buildconf%" >NUL
rmdir /Q /S "%cd%\ZeroRpc.Net\obj" >NUL

%msbuild% "%cd%\ZeroRpc.Net\ZeroRpc.Net.csproj" /p:Configuration=%buildconf%,Platform=%buildplat%

echo All done!
pause