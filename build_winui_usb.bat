@echo off
setlocal
echo Building WinUI unpackaged (USB)...
powershell -NoProfile -ExecutionPolicy Bypass -File "%~dp0build_winui_usb.ps1" %*
if errorlevel 1 (
  echo Build failed.
  exit /b 1
)
endlocal
