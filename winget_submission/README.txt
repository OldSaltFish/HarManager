# WinGet 发布指南

## 1. 准备文件
你的可执行文件位于：
`d:\code\pc\har\publish\HarManager.exe`

## 2. 上传文件
将 `HarManager.exe` 上传到一个公开可访问的 URL（例如 GitHub Releases）。
**注意**：WinGet 要求下载链接是直接指向 `.exe` 文件的直链。

## 3. 更新清单文件
打开 `d:\code\pc\har\winget_submission\HunQiMeng.HarManager.installer.yaml` 文件。
将 `InstallerUrl` 字段的值替换为你上传后的真实下载链接。

例如：
```yaml
InstallerUrl: https://github.com/yourname/har-manager/releases/download/v1.0.0/HarManager.exe
```
(如果你的文件内容有变，请重新计算 SHA256 哈希值并更新 `InstallerSha256` 字段)

## 4. 提交到 WinGet
推荐使用 `wingetcreate` 工具进行提交（如果没有安装，可以运行 `winget install wingetcreate`）。

在命令行中运行：
```powershell
wingetcreate submit -p d:\code\pc\har\winget_submission
```
或者，你可以手动 Fork [winget-pkgs](https://github.com/microsoft/winget-pkgs) 仓库，将这三个 yaml 文件放入 `manifests/h/HunQiMeng/HarManager/1.0.0` 目录，并提交 Pull Request。
