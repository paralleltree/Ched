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
    public partial class CustomLaneOffsetSelectionForm : Form
    {
        public decimal LaneOffset
        {
            get { return LaneOffsetBox.Value; }
            set
            {
                LaneOffsetBox.Value = value;
                LaneOffsetBox.SelectAll();
            }
        }

        public CustomLaneOffsetSelectionForm()
        {
            InitializeComponent();
            AcceptButton = buttonOK;
            CancelButton = buttonCancel;
            buttonOK.DialogResult = DialogResult.OK;
            buttonCancel.DialogResult = DialogResult.Cancel;

            LaneOffsetBox.Minimum = -100m;
            LaneOffsetBox.Maximum = 100m;
            LaneOffsetBox.Increment = 1m;
            LaneOffsetBox.DecimalPlaces = 0;
            LaneOffsetBox.Value = 0;
        }
    }
}
