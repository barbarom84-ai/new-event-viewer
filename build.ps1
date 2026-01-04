# Script de compilation optimisé pour l'Observateur d'Événements Moderne
# Exécutez ce script avec: .\build.ps1

Write-Host "════════════════════════════════════════════════════════" -ForegroundColor Cyan
Write-Host "  ⚡ Compilation de l'Observateur d'Événements Moderne  " -ForegroundColor Cyan
Write-Host "════════════════════════════════════════════════════════" -ForegroundColor Cyan
Write-Host ""

# Nettoyage
Write-Host "🧹 Nettoyage des anciens fichiers..." -ForegroundColor Yellow
if (Test-Path "bin") { Remove-Item -Recurse -Force "bin" }
if (Test-Path "obj") { Remove-Item -Recurse -Force "obj" }

Write-Host ""
Write-Host "🔨 Compilation en cours (mode optimisé)..." -ForegroundColor Yellow
Write-Host ""

# Compilation avec toutes les optimisations
$compileResult = dotnet publish -c Release -r win-x64 `
    --self-contained true `
    -p:PublishSingleFile=true `
    -p:PublishTrimmed=true `
    -p:TrimMode=link `
    -p:EnableCompressionInSingleFile=true `
    -p:IncludeNativeLibrariesForSelfExtract=true `
    -p:PublishReadyToRun=true `
    -p:DebugType=none `
    -p:DebugSymbols=false `
    --nologo

Write-Host ""
Write-Host "════════════════════════════════════════════════════════" -ForegroundColor Cyan

if ($LASTEXITCODE -eq 0) {
    Write-Host "✓ Compilation réussie !" -ForegroundColor Green
    Write-Host ""
    
    $exePath = "bin\Release\net8.0-windows\win-x64\publish\EventViewer.exe"
    
    Write-Host "📁 Emplacement de l'exécutable:" -ForegroundColor Green
    Write-Host "   $exePath" -ForegroundColor White
    Write-Host ""
    
    if (Test-Path $exePath) {
        $fileSize = (Get-Item $exePath).Length
        $fileSizeMB = [math]::Round($fileSize / 1MB, 2)
        
        Write-Host "📊 Caractéristiques:" -ForegroundColor Green
        Write-Host "   • Fichier unique portable" -ForegroundColor White
        Write-Host "   • Optimisé pour la taille et la vitesse" -ForegroundColor White
        Write-Host "   • Prêt pour clé USB" -ForegroundColor White
        Write-Host ""
        Write-Host "💾 Taille: $fileSizeMB MB" -ForegroundColor Green
        Write-Host ""
        
        # Proposer d'ouvrir l'emplacement
        $openFolder = Read-Host "Voulez-vous ouvrir l'emplacement du fichier ? (O/N)"
        if ($openFolder -eq "O" -or $openFolder -eq "o") {
            Start-Process (Split-Path $exePath -Parent)
        }
    }
} else {
    Write-Host "✗ Erreur lors de la compilation." -ForegroundColor Red
    Write-Host ""
    Write-Host "Vérifiez que:" -ForegroundColor Yellow
    Write-Host "  • .NET 8.0 SDK est installé" -ForegroundColor White
    Write-Host "  • Vous êtes dans le bon répertoire" -ForegroundColor White
}

Write-Host "════════════════════════════════════════════════════════" -ForegroundColor Cyan
Write-Host ""

