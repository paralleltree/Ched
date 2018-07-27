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
    public partial class CustomQuantizeSelectionForm : Form
    {
        private int BarTick { get; }

        public double QuantizeTick
        {
            get
            {
                return Math.Max(BarTick / Math.Pow(2, noteLengthBox.SelectedIndex) / (int)noteDivisionBox.Value, 1);
            }
        }

        public CustomQuantizeSelectionForm(int barTick)
        {
            InitializeComponent();
            Text = "カスタム音符指定";
            AcceptButton = buttonOK;
            CancelButton = buttonCancel;
            buttonOK.DialogResult = DialogResult.OK;
            buttonCancel.DialogResult = DialogResult.Cancel;

            BarTick = barTick;

            noteLengthBox.Items.AddRange(Enumerable.Range(0, 8).Select(p => ((int)Math.Pow(2, p)).ToString()).ToArray());
            noteLengthBox.SelectedIndex = 2;

            noteDivisionBox.Minimum = 1;
            noteDivisionBox.Maximum = 15;
            noteDivisionBox.Value = 1;
        }
    }
}
