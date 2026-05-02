# Python环境清理与安装自动化脚本
# 适用于Windows系统

param(
    [switch]$SkipCleanup,
    [switch]$SkipInstall,
    [string]$PythonInstallerPath = "",
    [string]$PythonInstallPath = "C:\Python314"
)

# 设置错误处理偏好
$ErrorActionPreference = "Continue"

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "  Python环境清理与安装工具" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

# 检查管理员权限
function Test-Admin {
    try {
        $currentPrincipal = New-Object Security.Principal.WindowsPrincipal([Security.Principal.WindowsIdentity]::GetCurrent())
        return $currentPrincipal.IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)
    } catch {
        return $false
    }
}

if (-not (Test-Admin)) {
    Write-Host "错误: 请以管理员身份运行此脚本!" -ForegroundColor Red
    Write-Host "右键点击脚本，选择'以管理员身份运行'" -ForegroundColor Yellow
    Write-Host ""
    Write-Host "按任意键退出..."
    $null = $Host.UI.RawUI.ReadKey("NoEcho,IncludeKeyDown")
    exit 1
}

# 清理Python历史版本
function Clear-PythonHistory {
    Write-Host "[1/6] 清理Python历史版本..." -ForegroundColor Yellow
    
    # 常见的Python安装目录
    $pythonPaths = @(
        "C:\Python*",
        "C:\Program Files\Python*",
        "C:\Program Files (x86)\Python*",
        "$env:LOCALAPPDATA\Programs\Python",
        "$env:APPDATA\Python"
    )
    
    foreach ($path in $pythonPaths) {
        try {
            if (Test-Path $path) {
                Write-Host "  删除: $path" -ForegroundColor Gray
                Remove-Item -Path $path -Recurse -Force -ErrorAction SilentlyContinue
            }
        } catch {
            Write-Host "  警告: 无法删除 $path - $_" -ForegroundColor Yellow
        }
    }
    
    # 清理pip缓存
    try {
        $pipCache = "$env:LOCALAPPDATA\pip"
        if (Test-Path $pipCache) {
            Write-Host "  删除pip缓存: $pipCache" -ForegroundColor Gray
            Remove-Item -Path $pipCache -Recurse -Force -ErrorAction SilentlyContinue
        }
    } catch {
        Write-Host "  警告: 无法删除pip缓存 - $_" -ForegroundColor Yellow
    }
    
    Write-Host "  完成!" -ForegroundColor Green
}

# 清理Python注册表项
function Clear-PythonRegistry {
    Write-Host "[2/6] 清理Python注册表..." -ForegroundColor Yellow
    
    $registryPaths = @(
        "HKCU:\Software\Python",
        "HKLM:\Software\Python",
        "HKLM:\Software\Wow6432Node\Python",
        "HKCU:\Software\Microsoft\Windows\CurrentVersion\Uninstall\Python*",
        "HKLM:\Software\Microsoft\Windows\CurrentVersion\Uninstall\Python*",
        "HKLM:\Software\Wow6432Node\Microsoft\Windows\CurrentVersion\Uninstall\Python*"
    )
    
    foreach ($regPath in $registryPaths) {
        try {
            if (Test-Path $regPath) {
                Write-Host "  删除注册表项: $regPath" -ForegroundColor Gray
                Remove-Item -Path $regPath -Recurse -Force -ErrorAction SilentlyContinue
            }
        } catch {
            Write-Host "  警告: 无法删除注册表项 $regPath - $_" -ForegroundColor Yellow
        }
    }
    
    Write-Host "  完成!" -ForegroundColor Green
}

# 清理环境变量中的Python路径
function Clear-PythonEnvVars {
    Write-Host "[3/6] 清理环境变量..." -ForegroundColor Yellow
    
    $envPathsToRemove = @(
        "Python",
        "pip"
    )
    
    # 清理用户环境变量
    try {
        $userPath = [Environment]::GetEnvironmentVariable("Path", "User")
        if ($userPath) {
            $pathParts = $userPath -split ';' | Where-Object { $_ -ne "" }
            $newPathParts = @()
            foreach ($part in $pathParts) {
                $keep = $true
                foreach ($keyword in $envPathsToRemove) {
                    if ($part -like "*$keyword*") {
                        $keep = $false
                        Write-Host "  从用户PATH移除: $part" -ForegroundColor Gray
                        break
                    }
                }
                if ($keep) {
                    $newPathParts += $part
                }
            }
            [Environment]::SetEnvironmentVariable("Path", ($newPathParts -join ';'), "User")
        }
    } catch {
        Write-Host "  警告: 清理用户环境变量时出错 - $_" -ForegroundColor Yellow
    }
    
    # 清理系统环境变量
    try {
        $systemPath = [Environment]::GetEnvironmentVariable("Path", "Machine")
        if ($systemPath) {
            $pathParts = $systemPath -split ';' | Where-Object { $_ -ne "" }
            $newPathParts = @()
            foreach ($part in $pathParts) {
                $keep = $true
                foreach ($keyword in $envPathsToRemove) {
                    if ($part -like "*$keyword*") {
                        $keep = $false
                        Write-Host "  从系统PATH移除: $part" -ForegroundColor Gray
                        break
                    }
                }
                if ($keep) {
                    $newPathParts += $part
                }
            }
            [Environment]::SetEnvironmentVariable("Path", ($newPathParts -join ';'), "Machine")
        }
    } catch {
        Write-Host "  警告: 清理系统环境变量时出错 - $_" -ForegroundColor Yellow
    }
    
    Write-Host "  完成!" -ForegroundColor Green
}

# 查找Python 3.14安装程序
function Find-PythonInstaller {
    Write-Host "[4/6] 查找Python 3.14安装程序..." -ForegroundColor Yellow
    
    if ($PythonInstallerPath -and (Test-Path $PythonInstallerPath)) {
        Write-Host "  使用指定的安装程序: $PythonInstallerPath" -ForegroundColor Green
        return $PythonInstallerPath
    }
    
    try {
        $scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
        $possibleInstallers = @(
            Join-Path $scriptDir "python-3.14*.exe",
            Join-Path $scriptDir "python-3.14*.msi",
            Join-Path $scriptDir "python*.exe",
            Join-Path $scriptDir "python*.msi"
        )
        
        foreach ($pattern in $possibleInstallers) {
            $installer = Get-ChildItem -Path $pattern -ErrorAction SilentlyContinue | Select-Object -First 1
            if ($installer) {
                Write-Host "  找到安装程序: $($installer.FullName)" -ForegroundColor Green
                return $installer.FullName
            }
        }
    } catch {
        Write-Host "  警告: 查找安装程序时出错 - $_" -ForegroundColor Yellow
    }
    
    Write-Host "  未找到Python安装程序!" -ForegroundColor Red
    Write-Host "  请将Python安装程序放在脚本同一目录下，或使用 -PythonInstallerPath 参数指定路径" -ForegroundColor Yellow
    return $null
}

# 安装Python 3.14
function Install-Python314 {
    param([string]$InstallerPath)
    
    Write-Host "[5/6] 安装Python 3.14..." -ForegroundColor Yellow
    
    if (-not (Test-Path $InstallerPath)) {
        Write-Host "  错误: 安装程序不存在!" -ForegroundColor Red
        return $false
    }
    
    Write-Host "  安装路径: $PythonInstallPath" -ForegroundColor Gray
    
    try {
        # 根据文件扩展名选择安装参数
        $ext = [System.IO.Path]::GetExtension($InstallerPath)
        
        if ($ext -eq ".exe") {
            $args = @(
                "/quiet",
                "InstallAllUsers=1",
                "PrependPath=0",
                "Include_test=0",
                "TargetDir=`"$PythonInstallPath`""
            )
        } elseif ($ext -eq ".msi") {
            $args = @(
                "/i",
                "`"$InstallerPath`"",
                "/quiet",
                "INSTALLDIR=`"$PythonInstallPath`"",
                "ALLUSERS=1"
            )
            $InstallerPath = "msiexec.exe"
        } else {
            Write-Host "  错误: 不支持的安装程序格式!" -ForegroundColor Red
            return $false
        }
        
        Write-Host "  正在安装..." -ForegroundColor Gray
        $process = Start-Process -FilePath $InstallerPath -ArgumentList $args -Wait -PassThru -NoNewWindow
        if ($process.ExitCode -eq 0 -or $process.ExitCode -eq 3010) {
            Write-Host "  安装成功!" -ForegroundColor Green
            return $true
        } else {
            Write-Host "  安装失败，退出代码: $($process.ExitCode)" -ForegroundColor Red
            return $false
        }
    } catch {
        Write-Host "  安装过程出错: $_" -ForegroundColor Red
        return $false
    }
}

# 配置环境变量
function Set-PythonEnvVars {
    Write-Host "[6/6] 配置环境变量..." -ForegroundColor Yellow
    
    $pythonExe = Join-Path $PythonInstallPath "python.exe"
    $scriptsPath = Join-Path $PythonInstallPath "Scripts"
    
    if (-not (Test-Path $pythonExe)) {
        Write-Host "  警告: 未找到Python可执行文件，尝试搜索..." -ForegroundColor Yellow
        # 尝试搜索Python安装位置
        $searchPaths = @(
            $PythonInstallPath,
            "C:\Python314",
            "C:\Program Files\Python314",
            "C:\Program Files (x86)\Python314",
            "$env:LOCALAPPDATA\Programs\Python\Python314"
        )
        foreach ($searchPath in $searchPaths) {
            $testPath = Join-Path $searchPath "python.exe"
            if (Test-Path $testPath) {
                $PythonInstallPath = $searchPath
                $pythonExe = $testPath
                $scriptsPath = Join-Path $searchPath "Scripts"
                Write-Host "  找到Python: $PythonInstallPath" -ForegroundColor Green
                break
            }
        }
    }
    
    if (-not (Test-Path $pythonExe)) {
        Write-Host "  错误: 未找到Python可执行文件!" -ForegroundColor Red
        return $false
    }
    
    try {
        # 获取当前系统PATH
        $systemPath = [Environment]::GetEnvironmentVariable("Path", "Machine")
        if (-not $systemPath) { $systemPath = "" }
        $pathParts = $systemPath -split ';' | Where-Object { $_ -ne "" }
        
        # 检查是否已添加
        $hasPythonPath = $false
        $hasScriptsPath = $false
        
        foreach ($part in $pathParts) {
            if ($part -eq $PythonInstallPath) { $hasPythonPath = $true }
            if ($part -eq $scriptsPath) { $hasScriptsPath = $true }
        }
        
        # 添加到系统PATH（开头）
        $newPathParts = @()
        if (-not $hasPythonPath) {
            $newPathParts += $PythonInstallPath
            Write-Host "  添加到PATH: $PythonInstallPath" -ForegroundColor Gray
        }
        if (-not $hasScriptsPath) {
            $newPathParts += $scriptsPath
            Write-Host "  添加到PATH: $scriptsPath" -ForegroundColor Gray
        }
        $newPathParts += $pathParts
        
        [Environment]::SetEnvironmentVariable("Path", ($newPathParts -join ';'), "Machine")
        
        Write-Host "  环境变量配置完成!" -ForegroundColor Green
        return $true
    } catch {
        Write-Host "  错误: 配置环境变量时出错 - $_" -ForegroundColor Red
        return $false
    }
}

# 主流程
try {
    if (-not $SkipCleanup) {
        Clear-PythonHistory
        Clear-PythonRegistry
        Clear-PythonEnvVars
    } else {
        Write-Host "[跳过] 清理步骤" -ForegroundColor Gray
    }
    
    if (-not $SkipInstall) {
        $installer = Find-PythonInstaller
        if ($installer) {
            $installSuccess = Install-Python314 -InstallerPath $installer
            if ($installSuccess) {
                Set-PythonEnvVars
            }
        }
    } else {
        Write-Host "[跳过] 安装步骤" -ForegroundColor Gray
    }
    
    Write-Host ""
    Write-Host "========================================" -ForegroundColor Cyan
    Write-Host "  所有操作完成!" -ForegroundColor Green
    Write-Host "========================================" -ForegroundColor Cyan
    Write-Host ""
    Write-Host "请重新打开终端或重启计算机以使环境变量生效" -ForegroundColor Yellow
    Write-Host "然后运行: python --version 验证安装" -ForegroundColor Gray
    
} catch {
    Write-Host ""
    Write-Host "========================================" -ForegroundColor Red
    Write-Host "  发生错误!" -ForegroundColor Red
    Write-Host "========================================" -ForegroundColor Red
    Write-Host ""
    Write-Host "错误信息: $_" -ForegroundColor Red
    Write-Host ""
    Write-Host "堆栈跟踪:" -ForegroundColor Yellow
    Write-Host $_.ScriptStackTrace -ForegroundColor Gray
}

Write-Host ""
Write-Host "按任意键退出..."
$null = $Host.UI.RawUI.ReadKey("NoEcho,IncludeKeyDown")
