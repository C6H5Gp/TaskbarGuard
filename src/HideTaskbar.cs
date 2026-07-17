using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;

class HideTaskbar
{
    [DllImport("user32.dll")] static extern bool EnumWindows(EnumWindowsProc lpEnumFunc, IntPtr lParam);
    [DllImport("user32.dll")] static extern int GetClassName(IntPtr hWnd, StringBuilder lpClassName, int nMaxCount);
    [DllImport("user32.dll")] static extern bool GetWindowRect(IntPtr hWnd, out RECT lpRect);
    [DllImport("shell32.dll")] static extern IntPtr SHAppBarMessage(uint dwMessage, ref APPBARDATA pData);
    [DllImport("user32.dll", CharSet = CharSet.Auto)]
    static extern int MessageBox(IntPtr hWnd, string text, string caption, uint type);

    delegate bool EnumWindowsProc(IntPtr hWnd, IntPtr lParam);

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

    const uint ABM_SETSTATE = 0x0000000A;
    const uint ABS_AUTOHIDE = 0x00000001;
    const uint MB_OK = 0;
    const uint MB_ICONINFORMATION = 0x40;

    static void Main(string[] args)
    {
        string cmd = args.Length > 0 ? args[0].ToLower() : "toggle";

        var handles = new List<TrayInfo>();

        EnumWindows((hWnd, lParam) =>
        {
            string cls = GetClassName(hWnd);
            if (cls == "Shell_TrayWnd" || cls == "Shell_SecondaryTrayWnd")
            {
                RECT r;
                GetWindowRect(hWnd, out r);
                handles.Add(new TrayInfo { hWnd = hWnd, cls = cls, rect = r });
            }
            return true;
        }, IntPtr.Zero);

        if (handles.Count == 0)
        {
            if (cmd == "toggle")
                MessageBox(IntPtr.Zero, "未找到任何任务栏窗口", "HideTaskbar", MB_OK | MB_ICONINFORMATION);
            return;
        }

        var messages = new List<string>();

        foreach (var t in handles)
        {
            bool isPrimary = (t.cls == "Shell_TrayWnd");
            string label = isPrimary ? "主屏" : "副屏 (" + (t.rect.L > 6000 ? "右" : t.rect.L > 3000 ? "中" : "") + ")";

            var abd = new APPBARDATA();
            abd.cbSize = Marshal.SizeOf(abd);
            abd.hWnd = t.hWnd;

            switch (cmd)
            {
                case "hide":
                    abd.lParam = (IntPtr)ABS_AUTOHIDE;
                    SHAppBarMessage(ABM_SETSTATE, ref abd);
                    messages.Add("已隐藏 " + label);
                    break;
                case "show":
                    abd.lParam = IntPtr.Zero;
                    SHAppBarMessage(ABM_SETSTATE, ref abd);
                    messages.Add("已显示 " + label);
                    break;
                case "toggle":
                default:
                    abd.lParam = (IntPtr)ABS_AUTOHIDE;
                    SHAppBarMessage(ABM_SETSTATE, ref abd);
                    messages.Add("已隐藏 " + label);
                    break;
            }
        }

        if (cmd == "toggle" && messages.Count > 0)
            MessageBox(IntPtr.Zero, string.Join("\n", messages), "HideTaskbar", MB_OK | MB_ICONINFORMATION);
    }

    static string GetClassName(IntPtr hWnd)
    {
        var sb = new StringBuilder(256);
        GetClassName(hWnd, sb, sb.Capacity);
        return sb.ToString();
    }

    class TrayInfo
    {
        public IntPtr hWnd;
        public string cls;
        public RECT rect;
    }
}
