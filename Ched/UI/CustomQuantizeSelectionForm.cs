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
        private int TicksPerBeat { get; }
        private int BarTick => TicksPerBeat * 4;

        protected int Denominator => (int)Math.Pow(2, noteLengthBox.SelectedIndex) * (int)noteDivisionBox.Value;
        public int QuantizeTick => Math.Max(BarTick / Denominator, 1);

        public CustomQuantizeSelectionForm(int ticksPerBeat)
        {
            InitializeComponent();
            Text = "カスタム音符指定";
            AcceptButton = buttonOK;
            CancelButton = buttonCancel;
            buttonOK.DialogResult = DialogResult.OK;
            buttonCancel.DialogResult = DialogResult.Cancel;

            TicksPerBeat = ticksPerBeat;

            noteLengthBox.Items.AddRange(Enumerable.Range(0, 8).Select(p => ((int)Math.Pow(2, p)).ToString()).ToArray());
            noteLengthBox.SelectedIndex = 2;

            noteDivisionBox.Minimum = 1;
            noteDivisionBox.Maximum = 15;
            noteDivisionBox.Value = 1;

            buttonOK.Click += (s, e) =>
            {
                var primes = Denominator.Factorize().ExceptAll(BarTick.Factorize()).ToList();
                if (primes.Count == 0) return;

                if (MessageBox.Show(this, string.Format("分解能に対応していない音符です。\nこの音符を利用するには分解能を{0}に指定してください。", BarTick * primes.Product()), Program.ApplicationName, MessageBoxButtons.OK, MessageBoxIcon.Warning) == DialogResult.OK)
                    DialogResult = DialogResult.None;
            };
        }
    }
}
