# TaskbarGuard

彻底隐藏 Windows 11 多屏任务栏，配合 MyDockFinder 使用。

## 原理

- **副屏**：设置 `MMTaskbarEnabled = 0`（系统级），`Shell_SecondaryTrayWnd` 根本不会被创建
- **主屏**：通过 `SHAppBarMessage(ABM_SETSTATE, ABS_AUTOHIDE)` 设置自动隐藏
- **守护**：`SetWinEventHook(EVENT_OBJECT_SHOW)` + 每秒轮询，防止 explorer 重置状态

## 文件

| 文件 | 说明 |
|------|------|
| `src/TaskbarGuard.cs` | 后台守护，开机自启，静默运行 |
| `src/HideTaskbar.cs` | 手动切换（`hide`/`show`/`toggle`） |

## 使用

### 后台守护（推荐）

```cmd
# 安装开机自启
TaskbarGuard.exe --install

# 移除开机自启（同时恢复多屏任务栏）
TaskbarGuard.exe --uninstall

# 直接运行（会先杀已有实例）
TaskbarGuard.exe
```

启动时自动设置 `MMTaskbarEnabled = 0`，无需手动改注册表。

### 手动切换

```cmd
HideTaskbar.exe        # 隐藏所有任务栏
HideTaskbar.exe hide   # 强制隐藏
HideTaskbar.exe show   # 恢复显示
```

## 编译

```cmd
C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe /nologo /out:TaskbarGuard.exe /target:winexe src\TaskbarGuard.cs
C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe /nologo /out:HideTaskbar.exe /target:winexe src\HideTaskbar.cs
```

.NET Framework 4.x 自带编译器，无需额外安装。`/target:winexe` 确保无控制台窗口。

## 许可

MIT
