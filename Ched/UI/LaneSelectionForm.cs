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
    public partial class LaneSelectionForm : Form
    {

        public int LanesCount
        {
            get { return (int)laneSelectBox.Value; }
            set
            {
                laneSelectBox.Value = value;

            }
        }

        public int MinusLanesCount
        {
            get { return (int)-minusLaneSelectBox.Value; }
            set
            {
                minusLaneSelectBox.Value = -value;

            }
        }

        public LaneSelectionForm()
        {
            InitializeComponent();
            AcceptButton = buttonOK;
            CancelButton = buttonCancel;
            buttonOK.DialogResult = DialogResult.OK;
            buttonCancel.DialogResult = DialogResult.Cancel;


            laneSelectBox.Minimum = 1;
            laneSelectBox.Maximum = 200;
            laneSelectBox.Increment = 2;
            laneSelectBox.DecimalPlaces = 0;
            laneSelectBox.Value = 1;

            minusLaneSelectBox.Minimum = 0;
            minusLaneSelectBox.Maximum = 100;
            minusLaneSelectBox.Increment = 2;
            minusLaneSelectBox.DecimalPlaces = 0;
            minusLaneSelectBox.Value = 0;
        }
    }
}
