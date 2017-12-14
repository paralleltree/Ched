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
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
#if !DEBUG
            Application.SetUnhandledExceptionMode(UnhandledExceptionMode.ThrowException);
            AppDomain.CurrentDomain.UnhandledException += (s, e) => DumpException((Exception)e.ExceptionObject, true);
#endif

            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new UI.MainForm());
        }

        public static void DumpException(Exception ex)
        {
            DumpException(ex, false);
        }

        public static void DumpException(Exception ex, bool forceClose)
        {
            File.WriteAllText("exception.json", Newtonsoft.Json.JsonConvert.SerializeObject(ex));
            if (!forceClose) return;
            try
            {
                MessageBox.Show("エラーが発生しました。\nアプリケーションを終了します。", "エラー");
            }
            finally
            {
                Environment.Exit(1);
            }
        }
    }
}
