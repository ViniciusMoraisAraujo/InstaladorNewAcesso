# publish.ps1
# Publica o EXE do InstaladorNewAcesso.Console como single-file no diretório dist/
#
# Uso:
#   .\scripts\publish.ps1                      # Publica Release, self-contained, win-x64
#   .\scripts\publish.ps1 -Configuration Debug   # Publica em Debug
#   .\scripts\publish.ps1 -SelfContained $false  # Publica framework-dependent (precisa .NET runtime)
#   .\scripts\publish.ps1 -OutputDir "C:\Temp"   # Diretório de saída customizado

param(
    [string]$Configuration = "Release",
    [bool]$SelfContained = $true,
    [string]$OutputDir = "",
    [string]$RuntimeIdentifier = "win-x64"
)

$rootDir = Split-Path -Parent $PSScriptRoot
if ([string]::IsNullOrWhiteSpace($OutputDir)) {
    $OutputDir = Join-Path $rootDir "dist"
}

Write-Host "══════════════════════════════════════════════════" -ForegroundColor Cyan
Write-Host "  InstaladorNewAcesso — Publicação Single-File" -ForegroundColor Cyan
Write-Host "══════════════════════════════════════════════════" -ForegroundColor Cyan
Write-Host ""
Write-Host "  Configuração : $Configuration"
Write-Host "  SelfContained : $SelfContained"
Write-Host "  Runtime       : $RuntimeIdentifier"
Write-Host "  Saída         : $OutputDir"
Write-Host ""

# Garantir que o diretório de saída existe
New-Item -ItemType Directory -Force -Path $OutputDir | Out-Null

# Lista de projetos EXE para publicar
$exeProjects = @(
    "src\InstaladorNewAcesso.Console\InstaladorNewAcesso.Console.csproj"
)

foreach ($proj in $exeProjects) {
    $projPath = Join-Path $rootDir $proj
    $projectName = Split-Path -LeafBase $projPath

    Write-Host "▶ Publicando $projectName ..." -ForegroundColor Yellow

    dotnet publish "$projPath" `
        --configuration $Configuration `
        --runtime $RuntimeIdentifier `
        --output "$OutputDir" `
        -p:PublishSingleFile=true `
        -p:IncludeNativeLibrariesForSelfExtract=true `
        -p:SelfContained=$SelfContained `
        -p:DebugType=none `
        --nologo

    if ($LASTEXITCODE -eq 0) {
        Write-Host "  ✅ $projectName publicado com sucesso!" -ForegroundColor Green
    } else {
        Write-Host "  ❌ Falha ao publicar $projectName (exit code: $LASTEXITCODE)" -ForegroundColor Red
    }
    Write-Host ""
}

Write-Host "══════════════════════════════════════════════════" -ForegroundColor Cyan
Write-Host "  Publicação concluída!" -ForegroundColor Cyan
Write-Host "══════════════════════════════════════════════════" -ForegroundColor Cyan
Write-Host ""

# Listar arquivos publicados
$distFiles = Get-ChildItem $OutputDir -Filter "*.exe" | Select-Object Name, Length
Write-Host "Arquivos gerados:" -ForegroundColor Cyan
foreach ($f in $distFiles) {
    $sizeMB = [math]::Round($f.Length / 1MB, 1)
    Write-Host "  $($f.Name)`t${sizeMB} MB"
}
Write-Host ""

# Total size
$totalSize = ($distFiles | Measure-Object -Property Length -Sum).Sum
$totalMB = [math]::Round($totalSize / 1MB, 1)
Write-Host "Tamanho total: ${totalMB} MB" -ForegroundColor Gray
