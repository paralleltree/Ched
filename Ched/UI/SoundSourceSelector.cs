using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Ched.UI
{
    public partial class SoundSourceSelector : UserControl
    {
        public IEnumerable<string> SupportedExtensions => new string[] { ".wav", ".mp3" };

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

            AllowDrop = true;
            DragEnter += (s, e) =>
            {
                e.Effect = DragDropEffects.None;
                if (!e.Data.GetDataPresent(DataFormats.FileDrop)) return;

                var items = (string[])e.Data.GetData(DataFormats.FileDrop);
                if (items.Length != 1) return;

                string path = items.Single();
                if (SupportedExtensions.Any(p => Path.GetExtension(path) == p) && File.Exists(path))
                    e.Effect = DragDropEffects.Copy;
            };
            DragDrop += (s, e) =>
            {
                filePathBox.Text = ((string[])e.Data.GetData(DataFormats.FileDrop)).Single();
            };
        }

        private void buttonBrowse_Click(object sender, EventArgs e)
        {
            var wildcards = SupportedExtensions.Select(p => "*" + p);
            var dialog = new OpenFileDialog()
            {
                Filter = string.Format("音声ファイル({0})|{1}", string.Join(", ", wildcards), string.Join(";", wildcards))
            };
            if (dialog.ShowDialog(this) == DialogResult.OK) filePathBox.Text = dialog.FileName;
        }
    }
}
