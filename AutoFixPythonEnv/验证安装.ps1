# Python安装验证脚本
Write-Host "========================================" -ForegroundColor Cyan
Write-Host "  Python环境验证工具" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

# 检查Python是否在PATH中
try {
    Write-Host "[1/3] 检查Python命令..." -ForegroundColor Yellow
    $pythonVersion = python --version 2>&1
    if ($LASTEXITCODE -eq 0) {
        Write-Host "  成功: $pythonVersion" -ForegroundColor Green
    } else {
        Write-Host "  警告: python命令未找到或执行失败" -ForegroundColor Yellow
    }
} catch {
    Write-Host "  错误: $_" -ForegroundColor Red
}

# 检查pip
try {
    Write-Host "[2/3] 检查pip命令..." -ForegroundColor Yellow
    $pipVersion = pip --version 2>&1
    if ($LASTEXITCODE -eq 0) {
        Write-Host "  成功: $pipVersion" -ForegroundColor Green
    } else {
        Write-Host "  警告: pip命令未找到或执行失败" -ForegroundColor Yellow
    }
} catch {
    Write-Host "  错误: $_" -ForegroundColor Red
}

# 检查常见安装位置
Write-Host "[3/3] 检查常见Python安装位置..." -ForegroundColor Yellow
$possiblePaths = @(
    "C:\Python314",
    "C:\Program Files\Python314",
    "C:\Program Files (x86)\Python314",
    "$env:LOCALAPPDATA\Programs\Python\Python314",
    "C:\Python*",
    "C:\Program Files\Python*"
)

$foundInstallations = @()
foreach ($pathPattern in $possiblePaths) {
    try {
        $paths = Get-ChildItem -Path $pathPattern -Directory -ErrorAction SilentlyContinue
        foreach $dir in $paths {
            $pythonExe = Join-Path $dir.FullName "python.exe"
            if (Test-Path $pythonExe) {
                if ($foundInstallations -notcontains $dir.FullName) {
                    $foundInstallations += $dir.FullName
                    Write-Host "  找到: $($dir.FullName)" -ForegroundColor Gray
                }
            }
        }
    } catch {}
}

if ($foundInstallations.Count -eq 0) {
    Write-Host "  未找到Python安装" -ForegroundColor Yellow
}

Write-Host ""
Write-Host "========================================" -ForegroundColor Cyan
Write-Host "  验证完成!" -ForegroundColor Green
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""
Write-Host "提示: 如果刚安装完Python，请重新打开终端或重启电脑" -ForegroundColor Yellow
Write-Host ""

Write-Host "按任意键退出..."
$null = $Host.UI.RawUI.ReadKey("NoEcho,IncludeKeyDown")