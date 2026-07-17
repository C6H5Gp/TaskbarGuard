using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;

class HideTaskbar
{
    [DllImport("user32.dll")] static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);
    [DllImport("user32.dll")] static extern bool IsWindowVisible(IntPtr hWnd);
    [DllImport("user32.dll")] static extern bool EnumWindows(EnumWindowsProc lpEnumFunc, IntPtr lParam);
    [DllImport("user32.dll")] static extern int GetClassName(IntPtr hWnd, StringBuilder lpClassName, int nMaxCount);
    [DllImport("user32.dll")] static extern bool GetWindowRect(IntPtr hWnd, out RECT lpRect);
    [DllImport("user32.dll")] static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);

    delegate bool EnumWindowsProc(IntPtr hWnd, IntPtr lParam);

    const int SW_HIDE = 0;
    const int SW_SHOW = 5;

    [StructLayout(LayoutKind.Sequential)]
    struct RECT { public int L, T, R, B; }

    static void Main(string[] args)
    {
        string cmd = args.Length > 0 ? args[0].ToLower() : "toggle";

        bool includePrimary = cmd == "--all" || (args.Length > 1 && args[1] == "--all");
        if (cmd == "--all") cmd = "toggle";

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
            Console.WriteLine("[!] 未找到任何任务栏窗口");
            Console.ReadKey();
            return;
        }

        foreach (var t in handles)
        {
            // 是否主显示器任务栏
            bool isPrimary = (t.cls == "Shell_TrayWnd");

            // 默认只操作副屏任务栏，除非指定 --all
            if (isPrimary && !includePrimary) continue;

            string label = isPrimary ? "主显示器" : string.Format("副屏 ({0})", t.rect.L < 0 ? "左" : "右");

            switch (cmd)
            {
                case "hide":
                    ShowWindow(t.hWnd, SW_HIDE);
                    Console.WriteLine(string.Format("[v] 已隐藏 {0}", label));
                    break;
                case "show":
                    ShowWindow(t.hWnd, SW_SHOW);
                    Console.WriteLine(string.Format("[v] 已显示 {0}", label));
                    break;
                case "toggle":
                default:
                    bool vis = IsWindowVisible(t.hWnd);
                    ShowWindow(t.hWnd, vis ? SW_HIDE : SW_SHOW);
                    Console.WriteLine(string.Format("[v] {0}: {1}", label, vis ? "可见 -> 隐藏" : "隐藏 -> 可见"));
                    break;
            }
        }

        if (cmd == "toggle") Console.WriteLine("\n按任意键退出...");
        if (cmd == "toggle") Console.ReadKey();
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
