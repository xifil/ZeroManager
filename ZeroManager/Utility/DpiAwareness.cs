using SkiaSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using Veldrid.Sdl2;

namespace ZeroManager.Utility {
    public class DpiAwareness {
        public static readonly int DPI_AWARENESS_CONTEXT_UNAWARE = -1;
        public static readonly int DPI_AWARENESS_CONTEXT_SYSTEM_AWARE = -2;
        public static readonly int DPI_AWARENESS_CONTEXT_PER_MONITOR_AWARE = -3;
        public static readonly int DPI_AWARENESS_CONTEXT_PER_MONITOR_AWARE_V2 = -4;
        public static readonly int DPI_AWARENESS_CONTEXT_UNAWARE_GDISCALED = -5;

        public static readonly int PROCESS_DPI_UNAWARE = 0;
        public static readonly int PROCESS_SYSTEM_DPI_AWARE = 1;
        public static readonly int PROCESS_PER_MONITOR_DPI_AWARE = 2;

        public static readonly uint MDT_Effective_DPI = 0;
        public static readonly int LOGPIXELSX = 88;
        public static readonly int LOGPIXELSY = 90;

        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        public static extern IntPtr LoadLibrary(string lpFileName);

        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        public static extern IntPtr GetProcAddress(IntPtr hModule, string lpProcName);

        [DllImport("user32.dll")]
        public static extern bool SetProcessDPIAware();

        [DllImport("user32.dll", SetLastError = true)]
        public static extern IntPtr GetDC(IntPtr hwnd);

        [DllImport("gdi32.dll")]
        public static extern int GetDeviceCaps(IntPtr hdc, int nIndex);

        [DllImport("user32.dll")]
        public static extern IntPtr MonitorFromWindow(IntPtr hwnd, uint dwFlags);

        [DllImport("shcore.dll")]
        public static extern int GetDpiForMonitor(IntPtr hmonitor, uint dpiType, out uint dpiX, out uint dpiY);

        delegate int PFN_SetThreadDpiAwarenessContext(int dpiAwarenessContext);
        delegate int PFN_SetProcessDpiAwareness(int dpiAwareness);

        public static bool IsWindows10OrGreater() {
            var version = Environment.OSVersion.Version;
            return version.Major >= 10;
        }

        public static bool IsWindows8Point1OrGreater() {
            var version = Environment.OSVersion.Version;
            return version.Major >= 6 && version.Minor >= 3;
        }

        public static void EnableDpiAwareness() {
            if (IsWindows10OrGreater()) {
                IntPtr user32Dll = LoadLibrary("user32.dll");
                if (user32Dll != IntPtr.Zero) {
                    IntPtr procAddress = GetProcAddress(user32Dll, "SetThreadDpiAwarenessContext");
                    if (procAddress != IntPtr.Zero) {
                        PFN_SetThreadDpiAwarenessContext SetThreadDpiAwarenessContextFn =
                            Marshal.GetDelegateForFunctionPointer<PFN_SetThreadDpiAwarenessContext>(procAddress);
                        SetThreadDpiAwarenessContextFn(DPI_AWARENESS_CONTEXT_PER_MONITOR_AWARE_V2);
                        return;
                    }
                }
            }

            if (IsWindows8Point1OrGreater()) {
                IntPtr shcoreDll = LoadLibrary("shcore.dll");
                if (shcoreDll != IntPtr.Zero) {
                    IntPtr procAddress = GetProcAddress(shcoreDll, "SetProcessDpiAwareness");
                    if (procAddress != IntPtr.Zero) {
                        PFN_SetProcessDpiAwareness SetProcessDpiAwarenessFn =
                            Marshal.GetDelegateForFunctionPointer<PFN_SetProcessDpiAwareness>(procAddress);
                        SetProcessDpiAwarenessFn(PROCESS_PER_MONITOR_DPI_AWARE);
                        return;
                    }
                }
            }

            SetProcessDPIAware();
        }

        public static float GetWindowScale(Sdl2Window? window) {
            if (window == null) {
                return 1f;
            }

            if (!OperatingSystem.IsWindows()) {
                return 1f;
            }

            IntPtr hwnd = window.Handle;
            if (hwnd == IntPtr.Zero) {
                return 1f;
            }

            IntPtr hmonitor = MonitorFromWindow(hwnd, 0);
            uint dpiX, dpiY;
            int result = GetDpiForMonitor(hmonitor, MDT_Effective_DPI, out dpiX, out dpiY);

            if (result == 0) {
                return (dpiX + dpiY) / (96f * 2f);
            }
            else {
                IntPtr hdc = GetDC(hwnd);
                int dpiXFallback = GetDeviceCaps(hdc, LOGPIXELSX);
                int dpiYFallback = GetDeviceCaps(hdc, LOGPIXELSY);
                return (dpiXFallback + dpiYFallback) / (96f * 2f);
            }
        }
    }
}
