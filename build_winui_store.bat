@echo off
setlocal
echo Building WinUI Store-flagged publish...
dotnet publish EventViewer.WinUI\EventViewer.WinUI.csproj -c Release -p:Platform=x64 -p:StoreBuild=true -p:WindowsPackageType=None --self-contained true -r win-x64 -o bin\WinUI_Store
if errorlevel 1 (
  echo Build failed.
  exit /b 1
)
echo.
echo Output: bin\WinUI_Store\EventViewer.WinUI.exe
echo Store mode: maintenance hidden, Store-friendly paths.
endlocal
