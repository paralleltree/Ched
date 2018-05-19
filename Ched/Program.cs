using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Threading.Tasks;
using System.Windows.Forms;

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
#if !DEBUG
            Application.SetUnhandledExceptionMode(UnhandledExceptionMode.ThrowException);
            AppDomain.CurrentDomain.UnhandledException += (s, e) => DumpException((Exception)e.ExceptionObject, true);
#endif

            if (!Properties.Settings.Default.HasUpgraded)
            {
                Properties.Settings.Default.Upgrade();
                Properties.Settings.Default.HasUpgraded = true;
                Properties.Settings.Default.Save();
            }
            if (!Properties.SoundConfiguration.Default.HasUpgraded)
            {
                Properties.SoundConfiguration.Default.Upgrade();
                Properties.SoundConfiguration.Default.HasUpgraded = true;
                Properties.SoundConfiguration.Default.Save();
            }

            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(args.Length == 0 ? new UI.MainForm() : new UI.MainForm(args[0]));
        }

        public static void DumpExceptionTo(Exception ex, string filename)
        {
            File.WriteAllText(filename, Newtonsoft.Json.JsonConvert.SerializeObject(ex));
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
    }
}
