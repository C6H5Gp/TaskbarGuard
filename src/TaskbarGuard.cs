using System;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using Microsoft.Win32;

class TaskbarGuard
{
    // --- Win32 ---
    [DllImport("user32.dll")] static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);
    [DllImport("user32.dll")] static extern bool IsWindowVisible(IntPtr hWnd);
    [DllImport("user32.dll")] static extern int GetClassName(IntPtr hWnd, StringBuilder lpClassName, int nMaxCount);
    [DllImport("user32.dll")] static extern bool EnumWindows(EnumWindowsProc lpEnumFunc, IntPtr lParam);
    [DllImport("user32.dll")] static extern IntPtr SetWinEventHook(uint eventMin, uint eventMax, IntPtr hmodWinEventProc,
        WinEventProc lpfnWinEventProc, uint idProcess, uint idThread, uint dwFlags);
    [DllImport("user32.dll")] static extern bool UnhookWinEvent(IntPtr hWinEventHook);
    [DllImport("user32.dll")] static extern IntPtr GetWindow(IntPtr hWnd, uint uCmd);
    [DllImport("user32.dll")] static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);

    delegate bool EnumWindowsProc(IntPtr hWnd, IntPtr lParam);
    delegate void WinEventProc(IntPtr hWinEventHook, uint eventType, IntPtr hWnd, int idObject,
        int idChild, uint dwEventThread, uint dwmsEventTime);

    const uint WINEVENT_OUTOFCONTEXT = 0;
    const uint EVENT_OBJECT_SHOW = 0x8002;
    const uint EVENT_OBJECT_CREATE = 0x8000;
    const int SW_HIDE = 0;

    static string GetClassName(IntPtr hWnd)
    {
        var sb = new StringBuilder(256);
        GetClassName(hWnd, sb, sb.Capacity);
        return sb.ToString();
    }

    static void Main(string[] args)
    {
        if (args.Length > 0 && args[0] == "--install")
        {
            Install();
            return;
        }
        if (args.Length > 0 && args[0] == "--uninstall")
        {
            Uninstall();
            return;
        }

        // Kill existing instance
        var current = System.Diagnostics.Process.GetCurrentProcess();
        foreach (var p in System.Diagnostics.Process.GetProcessesByName(current.ProcessName))
        {
            if (p.Id != current.Id) { try { p.Kill(); } catch { } }
        }

        Console.Title = "TaskbarGuard";
        Console.WriteLine("TaskbarGuard v5 - 后台守护已启动");
        Console.WriteLine("监控副屏任务栏窗口事件，自动隐藏...");
        Console.WriteLine("--install  安装开机自启");
        Console.WriteLine("--uninstall 移除开机自启");

        // 立即扫描一次
        HideSecondaryTaskbars();

        // 注册窗口事件钩子
        IntPtr hook = SetWinEventHook(EVENT_OBJECT_SHOW, EVENT_OBJECT_SHOW,
            IntPtr.Zero, OnWindowEvent, 0, 0, WINEVENT_OUTOFCONTEXT);

        if (hook == IntPtr.Zero)
        {
            Console.WriteLine("[!] SetWinEventHook 失败，切换到轮询模式");
            // 回退：轮询模式
            while (true)
            {
                Thread.Sleep(3000);
                HideSecondaryTaskbars();
            }
        }
        else
        {
            Console.WriteLine("[v] 事件钩子已注册");
            // 消息循环
            while (true)
            {
                // 同时每30秒兜底扫描一次
                Thread.Sleep(30000);
                HideSecondaryTaskbars();
            }
            // UnhookWinEvent(hook); // unreachable
        }
    }

    static void OnWindowEvent(IntPtr hWinEventHook, uint eventType, IntPtr hWnd,
        int idObject, int idChild, uint dwEventThread, uint dwmsEventTime)
    {
        if (idObject != 0 || idChild != 0) return;
        string cls = GetClassName(hWnd);
        if (cls == "Shell_SecondaryTrayWnd")
        {
            ShowWindow(hWnd, SW_HIDE);
        }
    }

    static void HideSecondaryTaskbars()
    {
        EnumWindows((hWnd, lParam) =>
        {
            string cls = GetClassName(hWnd);
            if (cls == "Shell_SecondaryTrayWnd" && IsWindowVisible(hWnd))
            {
                ShowWindow(hWnd, SW_HIDE);
            }
            return true;
        }, IntPtr.Zero);
    }

    static void Install()
    {
        string exePath = System.Reflection.Assembly.GetExecutingAssembly().Location;
        using (var key = Registry.CurrentUser.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Run", true))
        {
            key.SetValue("TaskbarGuard", string.Format("\"{0}\"", exePath));
        }
        Console.WriteLine("[v] 已添加开机自启: " + exePath);
        Console.ReadKey();
    }

    static void Uninstall()
    {
        using (var key = Registry.CurrentUser.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Run", true))
        {
            key.DeleteValue("TaskbarGuard", false);
        }
        Console.WriteLine("[v] 已移除开机自启");
        Console.ReadKey();
    }
}
