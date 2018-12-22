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
    public partial class HighSpeedSelectionForm : Form
    {
        public decimal SpeedRatio
        {
            get { return speedRatioBox.Value; }
            set { speedRatioBox.Value = value; }
        }

        public HighSpeedSelectionForm()
        {
            InitializeComponent();
            AcceptButton = buttonOK;
            CancelButton = buttonCancel;
            buttonOK.DialogResult = DialogResult.OK;
            buttonCancel.DialogResult = DialogResult.Cancel;

            speedRatioBox.Minimum = -100m;
            speedRatioBox.Maximum = 100m;
            speedRatioBox.Increment = 0.01m;
            speedRatioBox.DecimalPlaces = 2;
            speedRatioBox.Value = 1;
        }
    }
}
