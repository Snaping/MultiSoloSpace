# Clean-CDisk.ps1 - C盘清理自动化脚本

PowerShell脚本，用于自动清理Windows系统中的临时文件、缓存和垃圾文件。

## 功能特性

- 清理用户临时文件夹 (`%TEMP%`)
- 清理Windows系统临时文件夹 (`C:\Windows\Temp`)
- 清空回收站
- 清理Google Chrome浏览器缓存
- 清理Microsoft Edge浏览器缓存
- 可选：清理Windows更新缓存(WinSxS)

## 系统要求

- Windows操作系统
- PowerShell 5.0 或更高版本
- 管理员权限（仅WinSxS清理选项需要）

## 使用方法

### 基本清理

```powershell
.\Clean-CDisk.ps1
```

### 包含WinSxS清理（需要管理员权限）

```powershell
.\Clean-CDisk.ps1 -CleanWinSxS
```

## 以管理员身份运行

1. 右键点击PowerShell图标
2. 选择"以管理员身份运行"
3. 切换到脚本所在目录
4. 执行上述命令

## 注意事项

- 脚本会自动跳过不存在的文件路径
- 清理操作不可逆，请确保已保存重要文件
- 建议定期运行以保持系统清洁
- WinSxS清理需要管理员权限且可能耗时较长

## 清理项目详情

| 清理项目 | 路径 | 说明 |
|---------|------|------|
| 用户临时文件 | `%TEMP%` | 应用程序临时文件 |
| Windows临时文件 | `C:\Windows\Temp` | 系统临时文件 |
| 回收站 | - | 已删除的文件 |
| Chrome缓存 | `%LOCALAPPDATA%\Google\Chrome\User Data\Default\Cache` | 浏览器缓存 |
| Edge缓存 | `%LOCALAPPDATA%\Microsoft\Edge\User Data\Default\Cache` | 浏览器缓存 |
| WinSxS | `C:\Windows\WinSxS` | Windows更新缓存(可选) |

## 常见问题

**Q: 脚本运行时报错"无法加载文件"？**
A: 确保已启用PowerShell脚本执行策略，可运行：
```powershell
Set-ExecutionPolicy -ExecutionPolicy RemoteSigned -Scope CurrentUser
```

**Q: 清理后空间没有明显变化？**
A: 可能需要清理其他位置的大文件，如下载文件夹、临时安装文件等。

**Q: WinSxS清理失败？**
A: 确保以管理员身份运行PowerShell，并等待命令执行完成（可能需要几分钟）。