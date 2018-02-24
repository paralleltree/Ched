using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

using Ched.Components;
using Ched.Components.Exporter;

namespace Ched.UI
{
    public partial class ExportForm : Form
    {
        private readonly string ArgsKey = "sus";
        private readonly string Filter = "Seaurchin Score File(*.sus)|*.sus";

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
                if (string.IsNullOrEmpty(outputBox.Text)) browseButton.PerformClick();
                if (string.IsNullOrEmpty(outputBox.Text))
                {
                    MessageBox.Show(this, "出力先を指定してください。", "Ched");
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

                try
                {
                    new SusExporter().Export(outputBox.Text, book, args);
                    Close();
                }
                catch (Exception ex)
                {
                    MessageBox.Show(this, "エクスポートに失敗しました。", "エラー");
                    Program.DumpException(ex);
                }
            };
        }
    }
}
