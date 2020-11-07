using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows.Forms;

using Ched.Configuration;
using Ched.UI.Forms;

namespace Ched
{
    static class Program
    {
        internal static readonly string ApplicationName = System.Reflection.Assembly.GetEntryAssembly().GetName().Name;

        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main(string[] args)
        {
            Directory.SetCurrentDirectory(Path.GetDirectoryName(Application.ExecutablePath));

#if !DEBUG
            Application.SetUnhandledExceptionMode(UnhandledExceptionMode.ThrowException);
            AppDomain.CurrentDomain.UnhandledException += (s, e) => DumpException((Exception)e.ExceptionObject, true);
#endif

            AppDomain.CurrentDomain.AssemblyResolve += (s, e) =>
            {
                string path = Path.Combine(Plugins.PluginManager.PluginPath, new AssemblyName(e.Name).Name + ".dll");
                return File.Exists(path) ? Assembly.LoadFrom(path) : null;
            };

            UpgradeConfiguration(ApplicationSettings.Default);
            UpgradeConfiguration(SoundSettings.Default);

            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(args.Length == 0 ? new MainForm() : new MainForm(args[0]));
        }

        public static void DumpExceptionTo(Exception ex, string filename)
        {
            try
            {
                File.WriteAllText(filename, Newtonsoft.Json.JsonConvert.SerializeObject(ex));
            }
            catch (UnauthorizedAccessException)
            {
            }
        }

        public static void DumpException(Exception ex)
        {
            DumpException(ex, false);
        }

        public static void DumpException(Exception ex, bool forceClose)
        {
            DumpExceptionTo(ex, "exception.json");
            if (!forceClose) return;
            try
            {
                MessageBox.Show("エラーが発生しました。\nアプリケーションを終了します。", ApplicationName, MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                Environment.Exit(1);
            }
        }

        private static bool UpgradeConfiguration(SettingsBase setting)
        {
            try
            {
                if (!setting.HasUpgraded) setting.Upgrade();
            }
            catch (Exception ex)
            {
                DumpExceptionTo(ex, "configuration_exeption.json");
                setting.Reset();
                return false;
            }
            return true;
        }
    }
}
