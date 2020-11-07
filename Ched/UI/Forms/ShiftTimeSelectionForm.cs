using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

using Ched.Plugins;

namespace Ched.UI.Forms
{
    public partial class ShiftTimeSelectionForm : Form
    {
        private static readonly IReadOnlyList<(ShiftEvent.DurationType Type, string Text)> DurationTypes = new List<(ShiftEvent.DurationType, string)>()
        {
            (ShiftEvent.DurationType.Bar, "小節"),
            (ShiftEvent.DurationType.Beat, "拍")
        };

        public int CountValue => (int)countBox.Value;

        public ShiftEvent.DurationType DurationType => DurationTypes[durationTypeBox.SelectedIndex].Type;

        public ShiftTimeSelectionForm()
        {
            InitializeComponent();
            AcceptButton = buttonOK;
            CancelButton = buttonCancel;
            buttonOK.DialogResult = DialogResult.OK;
            buttonCancel.DialogResult = DialogResult.Cancel;

            countBox.Minimum = -10000;
            countBox.Maximum = 10000;
            countBox.Value = 1;
            durationTypeBox.DropDownStyle = ComboBoxStyle.DropDownList;
            durationTypeBox.Items.AddRange(DurationTypes.Select(p => p.Text).ToArray());
            durationTypeBox.SelectedIndex = 0;
        }
    }
}
