# TaskbarGuard

修复 MyDockFinder 多显示器环境下副屏任务栏无法完全隐藏的问题。

## 背景

MyDockFinder（Steam 上的 Dock/任务栏替代工具）在三显示器 + Windows 11 环境下存在 bug：DISPLAY1 副屏的 `Shell_SecondaryTrayWnd` 仅被设为自动隐藏模式（`ABS_AUTOHIDE`），而非彻底隐藏。导致鼠标触底或有系统通知时任务栏会弹出，体验不一致。

## 原理

- 通过 `SetWinEventHook` 监听 `EVENT_OBJECT_SHOW` 事件，副屏任务栏窗口一出现就调用 `ShowWindow(hWnd, SW_HIDE)` 彻底隐藏
- 每 30 秒兜底扫描，防止事件遗漏
- 仅操作 `Shell_SecondaryTrayWnd`（副屏任务栏），不影响主屏

## 文件

| 文件 | 说明 |
|------|------|
| `src/TaskbarGuard.cs` | 后台守护进程，事件驱动自动隐藏 |
| `src/HideTaskbar.cs` | 手动切换工具（`hide`/`show`/`toggle`） |

## 使用

### 后台守护（推荐）

```cmd
# 安装开机自启
TaskbarGuard.exe --install

# 移除开机自启
TaskbarGuard.exe --uninstall

# 直接运行（会先杀已有实例）
TaskbarGuard.exe
```

### 手动切换

```cmd
HideTaskbar.exe           # 切换副屏任务栏可见性
HideTaskbar.exe hide      # 强制隐藏
HideTaskbar.exe show      # 强制显示
HideTaskbar.exe toggle --all  # 含主屏
```

## 编译

```cmd
C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe /nologo /out:TaskbarGuard.exe /target:exe src\TaskbarGuard.cs
C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe /nologo /out:HideTaskbar.exe /target:exe src\HideTaskbar.cs
```

.NET Framework 4.x 自带编译器，无需额外安装。

## 许可

MIT
