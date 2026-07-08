@echo off
setlocal
echo Building WinUI unpackaged (USB)...
dotnet publish EventViewer.WinUI\EventViewer.WinUI.csproj -c Release -p:Platform=x64 -p:WindowsPackageType=None -p:PublishSingleFile=false --self-contained true -r win-x64 -o bin\WinUI_USB
if errorlevel 1 (
  echo Build failed.
  exit /b 1
)
echo.
echo Output: bin\WinUI_USB\EventViewer.WinUI.exe
echo Run as Administrator to read Windows event logs.
endlocal
