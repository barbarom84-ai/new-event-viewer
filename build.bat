@echo off
chcp 65001 >nul
echo ════════════════════════════════════════════════════════
echo   ⚡ Compilation de l'Observateur d'Événements Moderne
echo ════════════════════════════════════════════════════════
echo.

echo 🧹 Nettoyage des anciens fichiers...
if exist bin rmdir /s /q bin
if exist obj rmdir /s /q obj

echo.
echo 🔨 Compilation en cours (mode optimisé)...
echo.

dotnet publish -c Release -r win-x64 ^
    --self-contained true ^
    -p:PublishSingleFile=true ^
    -p:PublishTrimmed=true ^
    -p:TrimMode=link ^
    -p:EnableCompressionInSingleFile=true ^
    -p:IncludeNativeLibrariesForSelfExtract=true ^
    -p:PublishReadyToRun=true ^
    -p:DebugType=none ^
    -p:DebugSymbols=false

echo.
echo ════════════════════════════════════════════════════════
if %ERRORLEVEL% EQU 0 (
    echo ✓ Compilation réussie !
    echo.
    echo 📁 L'exécutable se trouve dans:
    echo    bin\Release\net8.0-windows\win-x64\publish\EventViewer.exe
    echo.
    echo 📊 Caractéristiques:
    echo    • Fichier unique portable
    echo    • Optimisé pour la taille et la vitesse
    echo    • Prêt pour clé USB
    echo.
    
    if exist "bin\Release\net8.0-windows\win-x64\publish\EventViewer.exe" (
        for %%A in ("bin\Release\net8.0-windows\win-x64\publish\EventViewer.exe") do (
            echo 💾 Taille: %%~zA octets
        )
    )
) else (
    echo ✗ Erreur lors de la compilation.
)
echo ════════════════════════════════════════════════════════
echo.
pause

