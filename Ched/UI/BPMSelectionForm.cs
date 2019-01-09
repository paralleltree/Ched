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
    public partial class BPMSelectionForm : Form
    {
        public decimal BPM
        {
            get { return bpmBox.Value; }
            set { bpmBox.Value = value; }
        }

        public BPMSelectionForm()
        {
            InitializeComponent();
            AcceptButton = buttonOK;
            CancelButton = buttonCancel;
            buttonOK.DialogResult = DialogResult.OK;
            buttonCancel.DialogResult = DialogResult.Cancel;

            bpmBox.DecimalPlaces = 0;
            bpmBox.Increment = 1;
            bpmBox.Maximum = 10000;
            bpmBox.Minimum = 10;
            bpmBox.Value = 120;
        }
    }
}
