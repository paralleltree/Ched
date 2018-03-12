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
        public decimal SpeedRatio { get { return speedRatioBox.Value; } }

        public HighSpeedSelectionForm()
        {
            InitializeComponent();
            Text = "ハイスピード速度指定";
            AcceptButton = buttonOK;
            CancelButton = buttonCancel;
            buttonOK.DialogResult = DialogResult.OK;
            buttonCancel.DialogResult = DialogResult.Cancel;

            speedRatioBox.Minimum = -9.99m;
            speedRatioBox.Maximum = 9.99m;
            speedRatioBox.Increment = 0.01m;
            speedRatioBox.DecimalPlaces = 2;
            speedRatioBox.Value = 1;
        }
    }
}
