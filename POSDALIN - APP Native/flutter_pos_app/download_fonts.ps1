# Download Noto Sans Khmer fonts from Google Fonts GitHub repository

$regularUrl = "https://github.com/google/fonts/raw/main/ofl/notosanskhmer/NotoSansKhmer%5Bwdth%2Cwght%5D.ttf"
$regularDest = "assets\fonts\NotoSansKhmer-Regular.ttf"

Write-Host "Downloading Noto Sans Khmer font files..."

try {
    # Download Regular font (variable font - includes all weights)
    Write-Host "Downloading Regular (variable) font..."
    Invoke-WebRequest -Uri $regularUrl -OutFile $regularDest -UseBasicParsing
    Write-Host "✓ Downloaded: $regularDest"
    
    # Copy the same file for Bold since it's a variable font
    Write-Host "Creating Bold font reference..."
    Copy-Item $regularDest -Destination "assets\fonts\NotoSansKhmer-Bold.ttf"
    Write-Host "✓ Created: assets\fonts\NotoSansKhmer-Bold.ttf"
    
    Write-Host "`nFont files downloaded successfully!"
    Write-Host "Location: $(Resolve-Path 'assets\fonts')"
    
} catch {
    Write-Error "Failed to download fonts: $_"
    Write-Host "`nAlternative: Download manually from:"
    Write-Host "https://fonts.google.com/noto/specimen/Noto+Sans+Khmer"
    exit 1
}
