@echo off
title Python环境清理与安装工具
chcp 65001 >nul
echo ========================================
echo   Python环境清理与安装工具
echo ========================================
echo.
echo 正在以管理员身份启动PowerShell脚本...
echo.

powershell -Command "Start-Process powershell -Verb RunAs -ArgumentList '-NoExit', '-NoProfile', '-ExecutionPolicy Bypass', '-Command', 'cd ''%~dp0''; & ''%~dp0FixPythonEnvironment.ps1'''"

echo.
echo 脚本已在新窗口中启动。
echo 如果窗口关闭，请检查是否授予了管理员权限。
echo.
pause
