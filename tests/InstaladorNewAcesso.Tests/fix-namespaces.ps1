$files = Get-ChildItem -Recurse -Filter "*.cs"
$count = 0
foreach ($file in $files) {
    $content = Get-Content $file.FullName -Raw
    $original = $content
    
    $content = $content -replace "using InstaladorNewAcesso\.Services;", "using InstaladorNewAcesso.Core.Services;"
    $content = $content -replace "using InstaladorNewAcesso\.Utils;", "using InstaladorNewAcesso.Core.Utils;"
    $content = $content -replace "using InstaladorNewAcesso\.Configurations;", "using InstaladorNewAcesso.Core.Configurations;"
    $content = $content -replace "using InstaladorNewAcesso\.Implementations;", "using InstaladorNewAcesso.Core.Implementations;"
    $content = $content -replace "using InstaladorNewAcesso\.Views;", "using InstaladorNewAcesso.Core.Views;"
    $content = $content -replace "using InstaladorNewAcesso\.Factories;", "using InstaladorNewAcesso.Core.Factories;"
    $content = $content -replace "using InstaladorNewAcesso\.Models;", "using InstaladorNewAcesso.Abstractions.Models;"
    $content = $content -replace "using InstaladorNewAcesso\.Interfaces;", "using InstaladorNewAcesso.Abstractions.Interfaces;"
    
    if ($content -ne $original) {
        Set-Content $file.FullName $content -NoNewline
        Write-Host "Updated: $($file.Name)"
        $count++
    }
}
Write-Host "Total files updated: $count"
