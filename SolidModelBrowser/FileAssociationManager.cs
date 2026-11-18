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

        public static string GetOrCreateExtensionIconPath(string extension)
        {
            try
            {
                string cleanExt = extension.TrimStart('.').ToLowerInvariant();
                string iconDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "SolidModelBrowser", "Icons");
                Directory.CreateDirectory(iconDir);
                string icoPath = Path.Combine(iconDir, $"{cleanExt}.ico");

                byte[] icoBytes = null;

                // 1. Try ResourceManager directly for embedded multi-resolution .ico
                try
                {
                    var rm = new System.Resources.ResourceManager("SolidModelBrowser.g", typeof(FileAssociationManager).Assembly);
                    using (var s = rm.GetStream($"icons/{cleanExt}.ico"))
                    {
                        if (s != null)
                        {
                            icoBytes = new byte[s.Length];
                            s.Read(icoBytes, 0, icoBytes.Length);
                        }
                    }
                }
                catch { }

                // 2. Try disk file in application directory
                if (icoBytes == null)
                {
                    string localIco = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Icons", $"{cleanExt}.ico");
                    if (File.Exists(localIco))
                        icoBytes = File.ReadAllBytes(localIco);
                }

                if (icoBytes != null && icoBytes.Length > 0)
                {
                    File.WriteAllBytes(icoPath, icoBytes);
                }

                if (File.Exists(icoPath)) return icoPath;
            }
            catch { }

            string exePath = Assembly.GetEntryAssembly()?.Location ?? System.Diagnostics.Process.GetCurrentProcess().MainModule?.FileName;
            return exePath;
        }

        public static bool SetAssociation(string extension, bool associate, string args = "\"%1\"")
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
                            string iconPath = GetOrCreateExtensionIconPath(extension);
                            if (iconPath.EndsWith(".ico", StringComparison.OrdinalIgnoreCase))
                                iconKey.SetValue("", $"\"{iconPath}\"");
                            else
                                iconKey.SetValue("", $"\"{iconPath}\",0");
                        }
                        using (var cmdKey = progKey.CreateSubKey(@"shell\open\command"))
                        {
                            string launchArgs = string.IsNullOrWhiteSpace(args) ? "\"%1\"" : args;
                            cmdKey.SetValue("", $"\"{exePath}\" {launchArgs}");
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
