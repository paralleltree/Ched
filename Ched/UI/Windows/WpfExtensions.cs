using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace Ched.UI.Windows
{
    public static class WpfExtensions
    {
        public static bool? ShowDialog(this Window window, System.Windows.Forms.Form form)
        {
            var helper = new System.Windows.Interop.WindowInteropHelper(window);
            helper.Owner = form.Handle;
            return window.ShowDialog();
        }
    }
}
