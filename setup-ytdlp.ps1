# Setup script to download yt-dlp.exe for the project

$ytDlpUrl = "https://github.com/yt-dlp/yt-dlp/releases/latest/download/yt-dlp.exe"
$ytDlpPath = Join-Path $PSScriptRoot "tools\yt-dlp.exe"
$toolsDir = Join-Path $PSScriptRoot "tools"

Write-Host "Setting up yt-dlp for TelegramYtDlpBot..." -ForegroundColor Cyan

# Create tools directory if it doesn't exist
if (-not (Test-Path $toolsDir)) {
    Write-Host "Creating tools directory..." -ForegroundColor Yellow
    New-Item -ItemType Directory -Path $toolsDir -Force | Out-Null
}

# Download yt-dlp.exe
if (Test-Path $ytDlpPath) {
    Write-Host "yt-dlp.exe already exists at: $ytDlpPath" -ForegroundColor Green
    $response = Read-Host "Do you want to re-download the latest version? (y/n)"
    if ($response -ne 'y') {
        Write-Host "Skipping download." -ForegroundColor Yellow
        exit 0
    }
}

Write-Host "Downloading yt-dlp.exe from GitHub..." -ForegroundColor Yellow
try {
    Invoke-WebRequest -Uri $ytDlpUrl -OutFile $ytDlpPath -UseBasicParsing
    Write-Host "✓ Successfully downloaded yt-dlp.exe to: $ytDlpPath" -ForegroundColor Green
    
    # Verify the file was downloaded
    if (Test-Path $ytDlpPath) {
        $fileSize = (Get-Item $ytDlpPath).Length / 1MB
        Write-Host "  File size: $($fileSize.ToString('0.00')) MB" -ForegroundColor Gray
    }
}
catch {
    Write-Host "✗ Failed to download yt-dlp.exe: $_" -ForegroundColor Red
    exit 1
}

Write-Host ""
Write-Host "Setup complete!" -ForegroundColor Green
Write-Host "To use yt-dlp in your project, update appsettings.json:" -ForegroundColor Cyan
Write-Host '  "YtDlp": {' -ForegroundColor Gray
Write-Host '    "ExecutablePath": "./tools/yt-dlp.exe"' -ForegroundColor Gray
Write-Host '  }' -ForegroundColor Gray
