using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Ched.UI
{
    internal static class ControlExtensions
    {
        public static LayoutManager WorkWithLayout(this Control control)
        {
            return new LayoutManager(control);
        }
    }

    internal class LayoutManager : IDisposable
    {
        protected Control _control;

        public LayoutManager(Control control)
        {
            control.SuspendLayout();
            _control = control;
        }

        public void Dispose()
        {
            _control.ResumeLayout(false);
            _control.PerformLayout();
        }
    }
}
