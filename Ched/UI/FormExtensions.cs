using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

using Ched.Localization;

namespace Ched.UI
{
    public static class FormExtensions
    {
        public static bool ConfirmDiscardChanges(this Form owner)
        {
            return MessageBox.Show(owner, ErrorStrings.FileDiscardConfirmation, Program.ApplicationName, MessageBoxButtons.OKCancel, MessageBoxIcon.Warning, MessageBoxDefaultButton.Button2) == DialogResult.OK;
        }
    }
}
