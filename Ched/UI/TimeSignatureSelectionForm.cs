using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Ched.UI
{
    public partial class TimeSignatureSelectionForm : Form
    {
        public int Numerator
        {
            get { return numeratorBox.SelectedIndex + 1; }
        }

        public int DenominatorExponent
        {
            get { return denominatorBox.SelectedIndex + 1; }
        }

        public TimeSignatureSelectionForm()
        {
            InitializeComponent();
            Text = "拍子の挿入";
            buttonOK.DialogResult = DialogResult.OK;
            buttonCancel.DialogResult = DialogResult.Cancel;

            numeratorBox.DropDownStyle = ComboBoxStyle.DropDownList;
            denominatorBox.DropDownStyle = ComboBoxStyle.DropDownList;
            numeratorBox.Items.AddRange(Enumerable.Range(1, 32).Select(p => p.ToString()).ToArray());
            denominatorBox.Items.AddRange(Enumerable.Range(1, 6).Select(p => Math.Pow(2, p).ToString()).ToArray());
            numeratorBox.SelectedIndex = 4 - 1;
            denominatorBox.SelectedIndex = 2 - 1;
        }
    }
}
