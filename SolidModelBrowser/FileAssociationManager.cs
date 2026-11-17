using System;
using System.IO;
using System.Reflection;
using Microsoft.Win32;

namespace SolidModelBrowser
{
    public static class FileAssociationManager
    {
        public static readonly string[] SupportedExtensions = { ".stl", ".3mf", ".obj", ".skp", ".ply", ".gcode" };

        public static bool IsAssociated(string extension)
        {
            try
            {
                using (var key = Registry.CurrentUser.OpenSubKey($@"Software\Classes\{extension}"))
                {
                    if (key == null) return false;
                    string progId = key.GetValue("") as string;
                    if (progId != $"SolidModelBrowser{extension}") return false;

                    using (var cmdKey = Registry.CurrentUser.OpenSubKey($@"Software\Classes\{progId}\shell\open\command"))
                    {
                        if (cmdKey == null) return false;
                        string cmd = cmdKey.GetValue("") as string;
                        string exePath = Assembly.GetEntryAssembly()?.Location ?? System.Diagnostics.Process.GetCurrentProcess().MainModule?.FileName;
                        return !string.IsNullOrEmpty(cmd) && !string.IsNullOrEmpty(exePath) && cmd.IndexOf(exePath, StringComparison.OrdinalIgnoreCase) >= 0;
                    }
                }
            }
            catch { return false; }
        }

        public static bool SetAssociation(string extension, bool associate)
        {
            try
            {
                string progId = $"SolidModelBrowser{extension}";
                string exePath = Assembly.GetEntryAssembly()?.Location ?? System.Diagnostics.Process.GetCurrentProcess().MainModule?.FileName;

                if (associate)
                {
                    if (string.IsNullOrEmpty(exePath) || !File.Exists(exePath)) return false;

                    using (var extKey = Registry.CurrentUser.CreateSubKey($@"Software\Classes\{extension}"))
                    {
                        extKey.SetValue("", progId);
                    }

                    using (var progKey = Registry.CurrentUser.CreateSubKey($@"Software\Classes\{progId}"))
                    {
                        progKey.SetValue("", $"Solid 3D Model ({extension.ToUpperInvariant()})");
                        using (var iconKey = progKey.CreateSubKey("DefaultIcon"))
                        {
                            iconKey.SetValue("", $"\"{exePath}\",0");
                        }
                        using (var cmdKey = progKey.CreateSubKey(@"shell\open\command"))
                        {
                            cmdKey.SetValue("", $"\"{exePath}\" \"%1\"");
                        }
                    }
                }
                else
                {
                    using (var extKey = Registry.CurrentUser.OpenSubKey($@"Software\Classes\{extension}", true))
                    {
                        if (extKey != null && extKey.GetValue("") as string == progId)
                        {
                            extKey.DeleteValue("", false);
                        }
                    }
                    Registry.CurrentUser.DeleteSubKeyTree($@"Software\Classes\{progId}", false);
                }

                SHChangeNotify(0x08000000, 0x0000, IntPtr.Zero, IntPtr.Zero); // SHCNE_ASSOCCHANGED
                return true;
            }
            catch { return false; }
        }

        [System.Runtime.InteropServices.DllImport("shell32.dll")]
        private static extern void SHChangeNotify(int wEventId, int uFlags, IntPtr dwItem1, IntPtr dwItem2);
    }
}
