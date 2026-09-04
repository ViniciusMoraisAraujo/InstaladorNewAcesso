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

Write-Host "==================================================" -ForegroundColor Cyan
Write-Host "  InstaladorNewAcesso - Publicacao Single-File" -ForegroundColor Cyan
Write-Host "==================================================" -ForegroundColor Cyan
Write-Host ""
Write-Host "  Configuracao  : $Configuration"
Write-Host "  SelfContained : $SelfContained"
Write-Host "  Runtime       : $RuntimeIdentifier"
Write-Host "  Saida         : $OutputDir"
Write-Host ""

New-Item -ItemType Directory -Force -Path $OutputDir | Out-Null

$projPath = Join-Path $rootDir "src\InstaladorNewAcesso.Console\InstaladorNewAcesso.Console.csproj"
$projectName = "InstaladorNewAcesso.Console"

Write-Host ">> Publicando $projectName ..." -ForegroundColor Yellow

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
    Write-Host "  [OK] $projectName publicado com sucesso!" -ForegroundColor Green
} else {
    Write-Host "  [ERRO] Falha ao publicar $projectName (exit code: $LASTEXITCODE)" -ForegroundColor Red
}

Write-Host ""
Write-Host "==================================================" -ForegroundColor Cyan
Write-Host "  Publicacao concluida!" -ForegroundColor Cyan
Write-Host "==================================================" -ForegroundColor Cyan
Write-Host ""

$distFiles = Get-ChildItem $OutputDir -Filter "*.exe" | Select-Object Name, Length
Write-Host "Arquivos gerados:" -ForegroundColor Cyan
foreach ($f in $distFiles) {
    $sizeMB = [math]::Round($f.Length / 1MB, 1)
    Write-Host "  $($f.Name) : $sizeMB MB"
}