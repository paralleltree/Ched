using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Ched.UI
{
    public partial class SoundSourceSelector : UserControl
    {
        public SoundSource Value
        {
            get { return new SoundSource(filePathBox.Text, (double)latencyBox.Value); }
            set
            {
                if (value == null) throw new ArgumentNullException();
                filePathBox.Text = value.FilePath;
                latencyBox.Value = (decimal)value.Latency;
            }
        }
        public SoundSourceSelector()
        {
            InitializeComponent();
        }

        private void buttonBrowse_Click(object sender, EventArgs e)
        {
            var dialog = new OpenFileDialog()
            {
                Filter = "音声ファイル(*.wav, *.mp3, *.m4a)|*.wav;*.mp3;*.m4a"
            };
            if (dialog.ShowDialog(this) == DialogResult.OK) filePathBox.Text = dialog.FileName;
        }
    }
}
