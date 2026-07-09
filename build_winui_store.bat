@echo off
setlocal
echo Building WinUI Store-flagged unpackaged...
powershell -NoProfile -ExecutionPolicy Bypass -File "%~dp0build_winui_store.ps1" %*
if errorlevel 1 (
  echo Build failed.
  exit /b 1
)
endlocal
