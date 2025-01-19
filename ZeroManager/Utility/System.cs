using Microsoft.Win32;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using Veldrid.Sdl2;

namespace ZeroManager.Utility {
    public class System {
        public const uint MB_OK = 0x0;
        public const uint MB_ICONINFORMATION = 0x40;

        public const int OFN_PATHMUSTEXIST = 0x00000800;
        public const int OFN_FILEMUSTEXIST = 0x00001000;
        public const int OFN_OVERWRITEPROMPT = 0x00000002;

        [StructLayout(LayoutKind.Sequential)]
        public class ITEMIDLIST {
            public ushort cb;
            public byte[] abID;
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
        public class OPENFILENAME
        {
            public int lStructSize = Marshal.SizeOf(typeof(OPENFILENAME));
            public IntPtr hwndOwner;
            public IntPtr hInstance;
            public string lpstrFilter = "All files\0*.*\0";
            public string lpstrCustomFilter;
            public int nMaxCustFilter;
            public int nFilterIndex;
            public string lpstrFile = new string(new char[256]);
            public int nMaxFile = 256;
            public string lpstrFileTitle = new string(new char[256]);
            public int nMaxFileTitle = 256;
            public string lpstrInitialDir;
            public string lpstrTitle = "Select a file";
            public int Flags = OFN_PATHMUSTEXIST | OFN_FILEMUSTEXIST | OFN_OVERWRITEPROMPT;
            public short nFileOffset;
            public short nFileExtension;
            public string lpstrDefExt;
            public IntPtr lCustData;
            public IntPtr lpfnHook;
            public string lpTemplateName;
            public IntPtr pvReserved;
            public int dwReserved;
            public int FlagsEx;
        }

        [DllImport("comdlg32.dll", CharSet = CharSet.Auto)]
        public static extern bool GetOpenFileName([In, Out] OPENFILENAME ofn);

        [DllImport("user32.dll", CharSet = CharSet.Ansi, SetLastError = true)]
        public static extern int MessageBoxA(IntPtr hWnd, string lpText, string lpCaption, uint uType);

        public static bool IsDarkMode() {
            const string registryKey = @"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize";
            const string registryValue = "AppsUseLightTheme";

            try {
                using (RegistryKey? key = Registry.CurrentUser.OpenSubKey(registryKey)) {
                    if (key != null) {
                        object? value = key.GetValue(registryValue);

                        if (value != null) {
                            return (int)value != 1;
                        }
                    }
                }
            }
            catch (Exception ex) {
                Console.WriteLine($"Error reading registry: {ex.Message}");
            }
            return false;
        }

        public static string? OpenFilePicker(Sdl2Window window, string title, string? filter = null) {
            OPENFILENAME ofn = new OPENFILENAME();
            ofn.hwndOwner = window.Handle;
            ofn.lpstrTitle = title;
            if (filter != null) {
                ofn.lpstrFilter = filter;
            }

            if (GetOpenFileName(ofn)) {
                return ofn.lpstrFile;
            }

            return null;
        }

        public static string ComputeSHA256Hash(byte[] data) {
            using (SHA256 sha256 = SHA256.Create()) {
                return BitConverter.ToString(sha256.ComputeHash(data)).Replace("-", "").ToLower();
            }
        }

        public static long GetFileSize(string path) {
            using (FileStream fileStream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite)) {
                return fileStream.Length;
            }
        }
    }
}
