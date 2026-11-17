using System;
using System.IO;
using System.Reflection;
using System.Windows;

namespace SolidModelBrowser
{
    public partial class App : Application
    {
        static App()
        {
            AppDomain.CurrentDomain.AssemblyResolve += (sender, args) =>
            {
                string assemblyName = new AssemblyName(args.Name).Name;
                var asm = Assembly.GetExecutingAssembly();
                foreach (var name in asm.GetManifestResourceNames())
                {
                    if (name.EndsWith(assemblyName + ".dll", StringComparison.OrdinalIgnoreCase))
                    {
                        using (Stream stream = asm.GetManifestResourceStream(name))
                        {
                            if (stream == null) return null;
                            byte[] buffer = new byte[stream.Length];
                            stream.Read(buffer, 0, buffer.Length);
                            return Assembly.Load(buffer);
                        }
                    }
                }
                return null;
            };
        }

        public static void SetTheme(string theme)
        {
            Current.Resources.Clear();

            ResourceDictionary rdColors = new ResourceDictionary();
            rdColors.Source = new Uri("pack://application:,,,/SolidModelBrowser;component/Styles/" + theme);
            Current.Resources.MergedDictionaries.Add(rdColors);

            ResourceDictionary rdStyles = new ResourceDictionary();
            rdStyles.Source = new Uri("pack://application:,,,/SolidModelBrowser;component/Styles/Styles.xaml");
            Current.Resources.MergedDictionaries.Add(rdStyles);
        }
    }
}
