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

                if (!File.Exists(icoPath))
                {
                    byte[] pngBytes = null;

                    // 1. Try WPF Pack URI
                    try
                    {
                        var uri = new Uri($"pack://application:,,,/Icons/{cleanExt}.png", UriKind.Absolute);
                        var sri = System.Windows.Application.GetResourceStream(uri);
                        if (sri != null && sri.Stream != null)
                        {
                            using (var s = sri.Stream)
                            {
                                pngBytes = new byte[s.Length];
                                s.Read(pngBytes, 0, pngBytes.Length);
                            }
                        }
                    }
                    catch { }

                    // 2. Try ResourceManager directly
                    if (pngBytes == null)
                    {
                        try
                        {
                            var rm = new System.Resources.ResourceManager("SolidModelBrowser.g", typeof(FileAssociationManager).Assembly);
                            using (var s = rm.GetStream($"icons/{cleanExt}.png"))
                            {
                                if (s != null)
                                {
                                    pngBytes = new byte[s.Length];
                                    s.Read(pngBytes, 0, pngBytes.Length);
                                }
                            }
                        }
                        catch { }
                    }

                    // 3. Fallback to disk file in application directory
                    if (pngBytes == null)
                    {
                        string localPng = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Icons", $"{cleanExt}.png");
                        if (File.Exists(localPng))
                            pngBytes = File.ReadAllBytes(localPng);
                    }

                    if (pngBytes != null && pngBytes.Length > 0)
                    {
                        using (var ms = new MemoryStream())
                        using (var bw = new BinaryWriter(ms))
                        {
                            bw.Write((UInt16)0);               // Reserved
                            bw.Write((UInt16)1);               // Type = Icon (1)
                            bw.Write((UInt16)1);               // Image count = 1
                            bw.Write((byte)16);                // Width = 16
                            bw.Write((byte)16);                // Height = 16
                            bw.Write((byte)0);                 // Color count = 0
                            bw.Write((byte)0);                 // Reserved = 0
                            bw.Write((UInt16)1);               // Planes = 1
                            bw.Write((UInt16)32);              // Bits per pixel = 32
                            bw.Write((UInt32)pngBytes.Length); // Size of PNG payload
                            bw.Write((UInt32)22);              // Offset of PNG payload (6 + 16 = 22)
                            bw.Write(pngBytes);                // Raw PNG data

                            File.WriteAllBytes(icoPath, ms.ToArray());
                        }
                    }
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
