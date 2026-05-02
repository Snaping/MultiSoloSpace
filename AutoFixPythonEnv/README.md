# Python环境清理与安装工具

一套完整的Windows Python环境自动化管理工具。

## 文件说明

| 文件 | 说明 |
|------|------|
| `运行脚本.bat` | 一键启动脚本（推荐使用） |
| `FixPythonEnvironment.ps1` | 主PowerShell脚本 |
| `验证安装.ps1` | 验证Python安装状态 |

## 功能特性

### 1. 清理功能
- 删除Python历史版本文件
- 清理注册表项
- 清理环境变量
- 清理pip缓存

### 2. 安装功能
- 自动查找Python安装程序
- 静默安装Python 3.14
- 自动配置环境变量

## 使用方法

### 快速开始

1. **下载Python安装程序**
   - 访问 [python.org](https://www.python.org/downloads/)
   - 下载Python 3.14安装程序（.exe或.msi格式）
   - 将安装程序放在本目录下

2. **运行脚本**
   - 右键点击 `运行脚本.bat`
   - 选择 **"以管理员身份运行"**
   - 等待脚本执行完成

3. **验证安装**
   - 重启终端或电脑
   - 运行 `验证安装.ps1` 检查状态

### 高级用法

#### PowerShell参数

```powershell
# 仅清理，不安装
.\FixPythonEnvironment.ps1 -SkipInstall

# 仅安装，不清理
.\FixPythonEnvironment.ps1 -SkipCleanup

# 指定安装程序路径
.\FixPythonEnvironment.ps1 -PythonInstallerPath "D:\Downloads\python-3.14.0.exe"

# 自定义安装目录
.\FixPythonEnvironment.ps1 -PythonInstallPath "D:\Python314"
```

#### 直接运行PowerShell脚本

```powershell
# 以管理员身份打开PowerShell
# 进入脚本目录
cd "D:\WorkSpace\Agent\MultiSoloSpace\MultiSoloSpace\AutoFixPythonEnv"

# 设置执行策略（如果需要）
Set-ExecutionPolicy -ExecutionPolicy Bypass -Scope Process

# 运行脚本
.\FixPythonEnvironment.ps1
```

## 常见问题

### Q: 提示需要管理员权限？
A: 是的，修改注册表和系统环境变量需要管理员权限。请右键点击脚本选择"以管理员身份运行"。

### Q: 找不到Python安装程序？
A: 请确保将Python安装程序放在脚本同一目录下，或使用 `-PythonInstallerPath` 参数指定路径。

### Q: 安装后运行python提示找不到命令？
A: 请重新打开终端或重启电脑，让环境变量生效。然后运行 `验证安装.ps1` 检查。

### Q: 窗口会自动关闭吗？
A: 不会。脚本使用 `-NoExit` 参数启动，并在结束时等待按键，确保窗口保持打开。

## 安全提示

- 脚本会删除Python相关文件，请确保已备份重要项目
- 建议在虚拟机或测试环境先试用
- 脚本不会删除用户的项目文件，只清理Python安装目录

## 许可证

本工具仅供学习和个人使用。
