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

namespace Ched.UI
{
    public partial class ExportForm : Form
    {
        private readonly string ArgsKey = "sus";
        private readonly string Filter = "Sliding Universal Score(*.sus)|*.sus";

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
            hasPaddingBarBox.Visible = false;

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

            browseButton.Click += (s, e) =>
            {
                var dialog = new SaveFileDialog()
                {
                    Filter = Filter
                };
                if (dialog.ShowDialog(this) == DialogResult.OK)
                {
                    outputBox.Text = dialog.FileName;
                }
            };

            exportButton.Click += (s, e) =>
            {
                if (string.IsNullOrEmpty(OutputPath)) browseButton.PerformClick();
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
