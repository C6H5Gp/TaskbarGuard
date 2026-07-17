using System;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using Microsoft.Win32;

class TaskbarGuard
{
    [DllImport("user32.dll")] static extern bool EnumWindows(EnumWindowsProc lpEnumFunc, IntPtr lParam);
    [DllImport("user32.dll")] static extern int GetClassName(IntPtr hWnd, StringBuilder lpClassName, int nMaxCount);
    [DllImport("user32.dll")] static extern IntPtr SetWinEventHook(uint eventMin, uint eventMax, IntPtr hmodWinEventProc,
        WinEventProc lpfnWinEventProc, uint idProcess, uint idThread, uint dwFlags);
    [DllImport("shell32.dll")] static extern IntPtr SHAppBarMessage(uint dwMessage, ref APPBARDATA pData);
    [DllImport("user32.dll", CharSet = CharSet.Auto)]
    static extern int MessageBox(IntPtr hWnd, string text, string caption, uint type);

    delegate bool EnumWindowsProc(IntPtr hWnd, IntPtr lParam);
    delegate void WinEventProc(IntPtr hWinEventHook, uint eventType, IntPtr hWnd, int idObject,
        int idChild, uint dwEventThread, uint dwmsEventTime);

    [StructLayout(LayoutKind.Sequential)] struct RECT { public int L, T, R, B; }
    [StructLayout(LayoutKind.Sequential)]
    struct APPBARDATA
    {
        public int cbSize;
        public IntPtr hWnd;
        public uint uCallbackMessage;
        public uint uEdge;
        public RECT rc;
        public IntPtr lParam;
    }

    const uint WINEVENT_OUTOFCONTEXT = 0;
    const uint EVENT_OBJECT_SHOW = 0x8002;
    const uint ABM_SETSTATE = 0x0000000A;
    const uint ABS_AUTOHIDE = 0x00000001;
    const uint MB_OK = 0;
    const uint MB_ICONINFORMATION = 0x40;

    static string GetClassName(IntPtr hWnd)
    {
        var sb = new StringBuilder(256);
        GetClassName(hWnd, sb, sb.Capacity);
        return sb.ToString();
    }

    static void SetAutoHide(IntPtr hWnd)
    {
        var abd = new APPBARDATA();
        abd.cbSize = Marshal.SizeOf(abd);
        abd.hWnd = hWnd;
        abd.lParam = (IntPtr)ABS_AUTOHIDE;
        SHAppBarMessage(ABM_SETSTATE, ref abd);
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

        // 禁止副屏任务栏（系统级，Shell_SecondaryTrayWnd 不会创建）
        using (var key = Registry.CurrentUser.OpenSubKey(
            @"SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer\Advanced", true))
        {
            key.SetValue("MMTaskbarEnabled", 0, RegistryValueKind.DWord);
        }

        // 立即设置所有任务栏自动隐藏
        HideAllTaskbars();

        // 注册窗口事件钩子
        IntPtr hook = SetWinEventHook(EVENT_OBJECT_SHOW, EVENT_OBJECT_SHOW,
            IntPtr.Zero, OnWindowEvent, 0, 0, WINEVENT_OUTOFCONTEXT);

        // 10秒兜底扫描（WinEvent 钩子负责实时响应）
        while (true)
        {
            Thread.Sleep(10000);
            HideAllTaskbars();
        }
    }

    static void OnWindowEvent(IntPtr hWinEventHook, uint eventType, IntPtr hWnd,
        int idObject, int idChild, uint dwEventThread, uint dwmsEventTime)
    {
        if (idObject != 0 || idChild != 0) return;
        string cls = GetClassName(hWnd);
        if (cls == "Shell_SecondaryTrayWnd" || cls == "Shell_TrayWnd")
        {
            SetAutoHide(hWnd);
        }
    }

    static void HideAllTaskbars()
    {
        EnumWindows((hWnd, lParam) =>
        {
            string cls = GetClassName(hWnd);
            if (cls == "Shell_SecondaryTrayWnd" || cls == "Shell_TrayWnd")
            {
                SetAutoHide(hWnd);
            }
            return true;
        }, IntPtr.Zero);
    }

    static void Install()
    {
        string exePath = System.Reflection.Assembly.GetExecutingAssembly().Location;
        using (var key = Registry.CurrentUser.OpenSubKey(
            @"SOFTWARE\Microsoft\Windows\CurrentVersion\Run", true))
        {
            key.SetValue("TaskbarGuard", string.Format("\"{0}\"", exePath));
        }
        MessageBox(IntPtr.Zero, "已添加开机自启:\n" + exePath, "TaskbarGuard", MB_OK | MB_ICONINFORMATION);
    }

    static void Uninstall()
    {
        using (var key = Registry.CurrentUser.OpenSubKey(
            @"SOFTWARE\Microsoft\Windows\CurrentVersion\Run", true))
        {
            key.DeleteValue("TaskbarGuard", false);
        }
        // 恢复副屏任务栏
        using (var key = Registry.CurrentUser.OpenSubKey(
            @"SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer\Advanced", true))
        {
            key.DeleteValue("MMTaskbarEnabled", false);
        }
        MessageBox(IntPtr.Zero, "已移除开机自启，已恢复多屏任务栏", "TaskbarGuard", MB_OK | MB_ICONINFORMATION);
    }
}
