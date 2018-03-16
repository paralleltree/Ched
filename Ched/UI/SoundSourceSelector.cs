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
            get
            {
                if (string.IsNullOrEmpty(filePathBox.Text)) return null;
                return new SoundSource(filePathBox.Text, (double)latencyBox.Value);
            }
            set
            {
                if (value == null) throw new ArgumentNullException();
                filePathBox.Text = value?.FilePath ?? "";
                latencyBox.Value = (decimal)(value?.Latency ?? 0);
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
                Filter = "音声ファイル(*.wav, *.mp3)|*.wav;*.mp3"
            };
            if (dialog.ShowDialog(this) == DialogResult.OK) filePathBox.Text = dialog.FileName;
        }
    }
}
