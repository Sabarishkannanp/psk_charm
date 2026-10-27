@echo off
setlocal
cd /d "%~dp0"

echo [1/3] Verifying payload archive...
if not exist "..\..\PSK_Charms_v1.0.zip" (
    echo Error: PSK_Charms_v1.0.zip not found!
    exit /b 1
)

set CSC=C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe
set NETDIR=C:\Windows\Microsoft.NET\Framework64\v4.0.30319
set WPFDIR=%NETDIR%\WPF

echo [2/3] Compiling PSK-Charms-Setup.exe...
"%CSC%" /nologo /target:winexe /optimize+ /platform:anycpu ^
    /out:"..\..\PSK-Charms-Setup.exe" ^
    /win32icon:"..\..\Assets\psk.ico" ^
    /win32manifest:"..\app.manifest" ^
    /resource:..\..\PSK_Charms_v1.0.zip,payload.zip ^
    /resource:..\..\Assets\psk_developer.png,avatar.png ^
    /r:"%WPFDIR%\PresentationCore.dll" ^
    /r:"%WPFDIR%\PresentationFramework.dll" ^
    /r:"%WPFDIR%\WindowsBase.dll" ^
    /r:"%NETDIR%\System.Xaml.dll" ^
    /r:"%NETDIR%\System.IO.Compression.dll" ^
    /r:"%NETDIR%\System.IO.Compression.FileSystem.dll" ^
    /r:System.dll ^
    /r:System.Core.dll ^
    /r:System.Drawing.dll ^
    Setup.cs

if %ERRORLEVEL% NEQ 0 (
    echo [ERROR] Setup build failed!
    exit /b %ERRORLEVEL%
)

echo [3/3] Created single standalone executable: d:\charm\PSK-Charms-Setup.exe
copy /Y "..\..\PSK-Charms-Setup.exe" "..\..\PSK_Charms_Installer.exe" >nul
echo Also copied to: d:\charm\PSK_Charms_Installer.exe
