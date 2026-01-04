@echo off
setlocal enabledelayedexpansion

echo ===================================================
echo   CONSTRUCTION VERSION USB - EVENT VIEWER
echo ===================================================
echo.

echo [1/3] Nettoyage des anciens fichiers...
if exist "bin" rd /s /q "bin"
if exist "obj" rd /s /q "obj"
echo Nettoyage termine.
echo.

echo [2/3] Compilation et Publication (Single File, Self-Contained)...
echo Cela peut prendre un moment selon votre processeur...
echo.

dotnet publish -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:PublishReadyToRun=true -p:IncludeNativeLibrariesForSelfExtract=true

if %errorlevel% neq 0 (
    echo.
    echo [!] ERREUR : La compilation a echoue.
    goto end
)

echo.
echo [3/3] Finalisation...
echo.
echo ===================================================
echo   CONSTRUCTION TERMINEE AVEC SUCCES !
echo ===================================================
echo.
echo Votre executable "tout-en-un" est disponible ici :
echo bin\Release\net8.0-windows\win-x64\publish\EventViewer.exe
echo.
echo CONSEIL : Copiez simplement ce fichier .exe sur votre cle USB.
echo.

:end
pause

