# Remove orphaned files from the original monolithic project
$orphans = @(
    "src/Program.cs",
    "src/InstaladorNewAcesso.csproj",
    "src/app.manifest",
    "src/Configurations",
    "src/Factories",
    "src/Implementations",
    "src/Interfaces",
    "src/Models",
    "src/Services",
    "src/Utils",
    "src/Views"
)

$removed = 0
foreach ($path in $orphans) {
    $fullPath = Join-Path (Get-Location) $path
    if (Test-Path $fullPath) {
        if ((Get-Item $fullPath) -is [System.IO.DirectoryInfo]) {
            Remove-Item -Recurse -Force $fullPath
            Write-Host "Removed directory: $path"
        } else {
            Remove-Item -Force $fullPath
            Write-Host "Removed file: $path"
        }
        $removed++
    }
}
Write-Host "Total removed: $removed"
