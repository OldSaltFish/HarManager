# HarManager

HarManager 是一个跨平台的 HAR (HTTP Archive) 文件查看器，基于 Avalonia UI 和 .NET 9 开发。它提供了现代化的三栏式界面，支持 HAR 文件导入、请求/响应详情查看、语法高亮以及请求管理功能。

## 1. 开发环境准备

在开始开发之前，请确保您的系统已安装以下工具：

- **.NET SDK 9.0**: [下载地址](https://dotnet.microsoft.com/download/dotnet/9.0)
- **IDE**: 推荐使用 VS Code (配合 C# Dev Kit 插件), Visual Studio 2022, 或 JetBrains Rider。

## 2. 运行与调试

### 命令行运行
在项目根目录下打开终端，执行以下命令启动应用：

```bash
dotnet run --project src/HarManager/HarManager.csproj
```

### 调试
- 如果使用 VS Code，直接按 `F5` 启动调试（需确保 `.vscode/launch.json` 配置正确，通常 C# 插件会自动生成）。
- 首次运行时，应用会在用户目录下创建 SQLite 数据库文件 (`~/.local/share/HarManager.db` 或 `%LocalStorage%/HarManager.db`)。

## 3. 构建与发布

为了获得最佳的性能和最小的体积，我们使用了一些优化参数（AOT 裁剪、去除全球化依赖等）。

### 发布 Linux 版本 (x64)

为了在 Linux 上获得低内存占用（<50MB）的效果，请使用以下命令进行发布。这会生成一个独立的、裁剪过的单文件可执行程序。

```bash
dotnet publish src/HarManager/HarManager.csproj \
    -c Release \
    -r linux-x64 \
    --self-contained \
    -p:PublishSingleFile=true \
    -p:PublishTrimmed=true \
    -p:InvariantGlobalization=true
```

**参数说明：**
- `-r linux-x64`: 指定目标平台为 Linux x64。
- `--self-contained`: 将 .NET 运行时打包在内，目标机器无需安装 .NET。
- `-p:PublishSingleFile=true`: 打包为单个可执行文件。
- `-p:PublishTrimmed=true`: 启用代码裁剪，移除未使用的库代码，显著减小体积。
- `-p:InvariantGlobalization=true`: 移除 ICU 全球化依赖。这是 **Linux 内存优化** 的关键，可减少约 50% 的内存占用。

### 发布 Windows 版本

```bash
dotnet publish src/HarManager/HarManager.csproj -c Release -r win-x64 --self-contained
```

## 4. 性能优化说明

针对 Linux 平台内存占用过高的问题，本项目已实施以下优化：

1.  **Invariant Globalization**: 禁用了系统级的 ICU 全球化支持（日期/字符串排序等），这在 Linux 上能节省大量内存。
2.  **Publish Trimmed**: 移除了未使用的 .NET 框架代码。
3.  **EF Core Lazy Loading**: 数据库查询改为懒加载模式，避免启动时一次性加载所有历史记录。

## 5. 常见问题

**Q: 启动时报错 `Process finished with exit code 139 (interrupted by signal 11: SIGSEGV)`**
A: 这通常是 Trim 裁剪过度导致的。如果遇到此问题，请尝试移除 `-p:PublishTrimmed=true` 参数重新发布。

**Q: 界面中文显示乱码**
A: 确保系统安装了中文字体。由于开启了 `InvariantGlobalization`，应用对特定区域设置的依赖已移除，但字体渲染仍依赖系统字体库。

