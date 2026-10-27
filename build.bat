@echo off
setlocal
cd /d "%~dp0"

echo [1/3] Setting up compiler environment...
set CSC=C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe
set NETDIR=C:\Windows\Microsoft.NET\Framework64\v4.0.30319
set WPFDIR=%NETDIR%\WPF

if not exist "%CSC%" (
    echo Error: C# compiler not found at %CSC%
    exit /b 1
)

echo [2/3] Compiling PSK Charms with native Windows .NET Framework 4.8...
"%CSC%" /nologo /target:winexe /optimize+ /platform:anycpu ^
    /out:"PSK.exe" ^
    /win32icon:"Assets\psk.ico" ^
    /win32manifest:"app.manifest" ^
    /r:"%WPFDIR%\PresentationCore.dll" ^
    /r:"%WPFDIR%\PresentationFramework.dll" ^
    /r:"%WPFDIR%\WindowsBase.dll" ^
    /r:"%NETDIR%\System.Xaml.dll" ^
    /r:"%NETDIR%\System.Web.Extensions.dll" ^
    /r:System.dll ^
    /r:System.Core.dll ^
    /r:System.Drawing.dll ^
    /r:System.Windows.Forms.dll ^
    src\AssemblyInfo.cs ^
    src\Win32Helper.cs ^
    src\AppSettings.cs ^
    src\CharmModel.cs ^
    src\PhysicsSimulator.cs ^
    src\OverlayWindow.cs ^
    src\ControlCenterWindow.cs ^
    src\TrayManager.cs ^
    src\Program.cs

if %ERRORLEVEL% NEQ 0 (
    echo [ERROR] Compilation failed!
    exit /b %ERRORLEVEL%
)

echo [3/3] Build succeeded: PSK.exe created successfully!
copy /Y "PSK.exe" "..\PSK.exe" >nul
echo Copied PSK.exe to root directory d:\charm\PSK.exe
