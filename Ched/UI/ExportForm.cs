using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

using Ched.Core;
using Ched.Components.Exporter;
using Ched.Localization;
using System.IO;

namespace Ched.UI
{
    public partial class ExportForm : Form
    {
        private readonly string ArgsKey = "sus";

        private SusExporter exporter = new SusExporter();

        public string OutputPath
        {
            get { return outputBox.Text; }
            set { outputBox.Text = value; }
        }

        public IExporter Exporter { get { return exporter; } }

        public ExportForm(ScoreBook book)
        {
            InitializeComponent();
            Icon = Properties.Resources.MainIcon;
            ShowInTaskbar = false;

            levelDropDown.Items.AddRange(Enumerable.Range(1, 14).SelectMany(p => new string[] { p.ToString(), p + "+" }).ToArray());
            difficultyDropDown.Items.AddRange(new string[] { "BASIC", "ADVANCED", "EXPERT", "MASTER", "WORLD'S END" });

            if (!book.ExporterArgs.ContainsKey(ArgsKey) || !(book.ExporterArgs[ArgsKey] is SusArgs))
            {
                book.ExporterArgs[ArgsKey] = new SusArgs();
            }

            var args = book.ExporterArgs[ArgsKey] as SusArgs;

            titleBox.Text = book.Title;
            artistBox.Text = book.ArtistName;
            notesDesignerBox.Text = book.NotesDesignerName;
            difficultyDropDown.SelectedIndex = (int)args.PlayDifficulty;
            levelDropDown.Text = args.PlayLevel;
            songIdBox.Text = args.SongId;
            soundFileBox.Text = args.SoundFileName;
            soundOffsetBox.Value = args.SoundOffset;
            jacketFileBox.Text = args.JacketFilePath;
            hasPaddingBarBox.Checked = args.HasPaddingBar;
            bgFileBox.Text = args.BgFilePath;            

            browseBgButton.Click += (s, e) =>
            {
                var dialog = new OpenFileDialog()
                {
                    Filter = "Background file(*.JPG;*.JPEG;*.PNG;*.MP4;) | *.JPG;*.JPEG;*.PNG;*.MP4; | All files (*.*) | *.* "
                };
                if (dialog.ShowDialog(this) == DialogResult.OK)
                {
                    bgFileBox.Text = Path.GetFileName(dialog.FileName);
                }
            };

            browseSoundButton.Click += (s, e) =>
            {
                var dialog = new OpenFileDialog()
                {
                    Filter = "Sound file(*.MP3;*.WAV;*.OGG;) | *.MP3;*.WAV;*.OGG; | All files (*.*) | *.* "
                };
                if (dialog.ShowDialog(this) == DialogResult.OK)
                {
                    soundFileBox.Text = Path.GetFileName(dialog.FileName);
                }
            };

            browseJacketButton.Click += (s, e) =>
            {
                var dialog = new OpenFileDialog()
                {
                    Filter = "Jacket file(*.JPG;*.JPEG；*.PNG) | *.JPG;*.JPEG；*.PNG | All files (*.*) | *.* "
                };
                if (dialog.ShowDialog(this) == DialogResult.OK)
                {
                    jacketFileBox.Text = Path.GetFileName(dialog.FileName);
                }
            };

            browseOutputButton.Click += (s, e) =>
            {
                var dialog = new SaveFileDialog()
                {
                    Filter = "Seaurchin Score File(*.sus)|*.sus"
                };
                if (dialog.ShowDialog(this) == DialogResult.OK)
                {
                    outputBox.Text = dialog.FileName;
                }
            };

            exportButton.Click += (s, e) =>
            {
                if (string.IsNullOrEmpty(OutputPath)) browseOutputButton.PerformClick();
                if (string.IsNullOrEmpty(OutputPath))
                {
                    MessageBox.Show(this, ErrorStrings.OutputPathRequired, Program.ApplicationName);
                    return;
                }
                book.Title = titleBox.Text;
                book.ArtistName = artistBox.Text;
                book.NotesDesignerName = notesDesignerBox.Text;
                args.PlayDifficulty = (SusArgs.Difficulty)difficultyDropDown.SelectedIndex;
                args.PlayLevel = levelDropDown.Text;
                args.SongId = songIdBox.Text;
                args.SoundFileName = soundFileBox.Text;
                args.SoundOffset = soundOffsetBox.Value;
                args.JacketFilePath = jacketFileBox.Text;
                args.HasPaddingBar = hasPaddingBarBox.Checked;
                args.BgFilePath = bgFileBox.Text;
                args.MovieOffset = movieOffsetBox.Value;
                
                try
                {
                    exporter.CustomArgs = args;
                    exporter.Export(OutputPath, book);
                    DialogResult = DialogResult.OK;
                    Close();
                }
                catch (Exception ex)
                {
                    MessageBox.Show(this, ErrorStrings.ExportFailed, Program.ApplicationName, MessageBoxButtons.OK, MessageBoxIcon.Error);
                    Program.DumpException(ex);
                }
            };
        }
    }
}
