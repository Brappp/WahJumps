# PowerShell script to download CSV files

$csvDir = Join-Path $PSScriptRoot "WahJumps\Data\CSV"

# Ensure the directory exists
if (-Not (Test-Path $csvDir)) {
    New-Item -Path $csvDir -ItemType Directory
}

# List of CSV URLs and output file names
$csvFiles = @(
    # NA Data Centers
    @{ Url = "https://docs.google.com/spreadsheets/d/1DyOqqECaNuAEntBxwv2NQ7p5rTrC1tDN9hHpcI_PNs4/export?format=csv&gid=82382952"; Output = "aether_cleaned.csv" },
    @{ Url = "https://docs.google.com/spreadsheets/d/1DyOqqECaNuAEntBxwv2NQ7p5rTrC1tDN9hHpcI_PNs4/export?format=csv&gid=1186977950"; Output = "primal_cleaned.csv" },
    @{ Url = "https://docs.google.com/spreadsheets/d/1DyOqqECaNuAEntBxwv2NQ7p5rTrC1tDN9hHpcI_PNs4/export?format=csv&gid=350373672"; Output = "crystal_cleaned.csv" },
    @{ Url = "https://docs.google.com/spreadsheets/d/1DyOqqECaNuAEntBxwv2NQ7p5rTrC1tDN9hHpcI_PNs4/export?format=csv&gid=1383994086"; Output = "dynamis_cleaned.csv" },
    
    # EU Data Centers
    @{ Url = "https://docs.google.com/spreadsheets/d/1DyOqqECaNuAEntBxwv2NQ7p5rTrC1tDN9hHpcI_PNs4/export?format=csv&gid=1339692877"; Output = "chaos_cleaned.csv" },
    @{ Url = "https://docs.google.com/spreadsheets/d/1DyOqqECaNuAEntBxwv2NQ7p5rTrC1tDN9hHpcI_PNs4/export?format=csv&gid=175977131"; Output = "light_cleaned.csv" },
    
    # OCE Data Center
    @{ Url = "https://docs.google.com/spreadsheets/d/1DyOqqECaNuAEntBxwv2NQ7p5rTrC1tDN9hHpcI_PNs4/export?format=csv&gid=874557131"; Output = "materia_cleaned.csv" },

    # JP Data Centers
    @{ Url = "https://docs.google.com/spreadsheets/d/1DyOqqECaNuAEntBxwv2NQ7p5rTrC1tDN9hHpcI_PNs4/export?format=csv&gid=1856583868"; Output = "elemental_cleaned.csv" },
    @{ Url = "https://docs.google.com/spreadsheets/d/1DyOqqECaNuAEntBxwv2NQ7p5rTrC1tDN9hHpcI_PNs4/export?format=csv&gid=1822506732"; Output = "gaia_cleaned.csv" },
    @{ Url = "https://docs.google.com/spreadsheets/d/1DyOqqECaNuAEntBxwv2NQ7p5rTrC1tDN9hHpcI_PNs4/export?format=csv&gid=1045300014"; Output = "mana_cleaned.csv" },
    @{ Url = "https://docs.google.com/spreadsheets/d/1DyOqqECaNuAEntBxwv2NQ7p5rTrC1tDN9hHpcI_PNs4/export?format=csv&gid=1643199164"; Output = "meteor_cleaned.csv" }
)

# Download each CSV file
foreach ($csv in $csvFiles) {
    $outputPath = Join-Path $csvDir $csv.Output
    Invoke-WebRequest -Uri $csv.Url -OutFile $outputPath
    Write-Host "Downloaded $($csv.Output) to $csvDir"
}
