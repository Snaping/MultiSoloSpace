param(
    [switch]$CleanWinSxS
)

Write-Host "==========================================" -ForegroundColor Cyan
Write-Host "     C Disk Cleanup Automation Script" -ForegroundColor Cyan
Write-Host "==========================================" -ForegroundColor Cyan
Write-Host ""

function Clean-TempFolder {
    param(
        [string]$Path,
        [string]$Description
    )

    if (Test-Path $Path) {
        Write-Host "Cleaning $Description..." -ForegroundColor Yellow
        $size = (Get-ChildItem -Path $Path -Recurse -ErrorAction SilentlyContinue | Measure-Object -Property Length -Sum).Sum
        $sizeGB = [math]::Round($size / 1GB, 2)
        $sizeMB = [math]::Round($size / 1MB, 2)

        if ($sizeGB -ge 1) {
            Write-Host "  Size: $sizeGB GB" -ForegroundColor White
        } else {
            Write-Host "  Size: $sizeMB MB" -ForegroundColor White
        }

        Remove-Item -Path "$Path\*" -Recurse -Force -ErrorAction SilentlyContinue
        Write-Host "  [OK] Completed" -ForegroundColor Green
        Write-Host ""
    } else {
        Write-Host "  Path does not exist, skipping" -ForegroundColor Gray
        Write-Host ""
    }
}

function Clean-BrowserCache {
    param(
        [string]$Path,
        [string]$BrowserName
    )

    if (Test-Path $Path) {
        Write-Host "Cleaning $BrowserName cache..." -ForegroundColor Yellow
        $size = (Get-ChildItem -Path $Path -Recurse -ErrorAction SilentlyContinue | Measure-Object -Property Length -Sum).Sum
        $sizeMB = [math]::Round($size / 1MB, 2)

        Write-Host "  Cache size: $sizeMB MB" -ForegroundColor White
        Remove-Item -Path "$Path\*" -Recurse -Force -ErrorAction SilentlyContinue
        Write-Host "  [OK] Completed" -ForegroundColor Green
        Write-Host ""
    } else {
        Write-Host "  $BrowserName is not installed or cache path does not exist" -ForegroundColor Gray
        Write-Host ""
    }
}

Clean-TempFolder -Path $env:TEMP -Description "User Temp Folder"

Clean-TempFolder -Path "C:\Windows\Temp" -Description "Windows Temp Folder"

Write-Host "Emptying Recycle Bin..." -ForegroundColor Yellow
Clear-RecycleBin -Force -ErrorAction SilentlyContinue
Write-Host "  [OK] Recycle Bin emptied" -ForegroundColor Green
Write-Host ""

Clean-BrowserCache -Path "$env:LOCALAPPDATA\Google\Chrome\User Data\Default\Cache" -BrowserName "Google Chrome"

Clean-BrowserCache -Path "$env:LOCALAPPDATA\Microsoft\Edge\User Data\Default\Cache" -BrowserName "Microsoft Edge"

if ($CleanWinSxS) {
    Write-Host "Cleaning Windows Update Cache (WinSxS)..." -ForegroundColor Yellow
    Write-Host "  Note: This operation requires administrator privileges" -ForegroundColor White
    dism /online /Cleanup-Image /StartComponentCleanup
    Write-Host "  [OK] WinSxS cleanup completed" -ForegroundColor Green
    Write-Host ""
}

Write-Host "==========================================" -ForegroundColor Cyan
Write-Host "     Cleanup Completed!" -ForegroundColor Green
Write-Host "==========================================" -ForegroundColor Cyan
Write-Host ""
Write-Host "Current C Drive Space Usage:" -ForegroundColor Yellow
Get-PSDrive C | Select-Object UsedGB, FreeGB | Format-Table -AutoSize

Write-Host ""
Write-Host "Usage Instructions:" -ForegroundColor Cyan
Write-Host '  - Normal cleanup: .\Clean-CDisk.ps1' -ForegroundColor White
Write-Host '  - Include WinSxS cleanup (requires admin): .\Clean-CDisk.ps1 -CleanWinSxS' -ForegroundColor White