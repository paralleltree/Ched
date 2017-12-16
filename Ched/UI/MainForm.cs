using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

using Ched.Components;
using Ched.Components.Notes;
using Ched.Properties;

namespace Ched.UI
{
    public partial class MainForm : Form
    {
        private readonly string FileTypeFilter = "Ched専用形式(*.chs)|*.chs";

        private ScoreBook ScoreBook { get; set; }

        private ScrollBar NoteViewScrollBar { get; }
        private NoteView NoteView { get; }

        public MainForm()
        {
            InitializeComponent();
            Size = new Size(400, 700);
            Icon = Resources.MainIcon;
            SetText();

            ToolStripManager.RenderMode = ToolStripManagerRenderMode.System;

            NoteView = new NoteView() { Dock = DockStyle.Fill };

            NoteViewScrollBar = new VScrollBar()
            {
                Dock = DockStyle.Right,
                Maximum = 0,
                Minimum = -NoteView.UnitBeatTick * 4 * 20,
                SmallChange = NoteView.UnitBeatTick
            };

            NoteView.Resize += (s, e) =>
            {
                NoteViewScrollBar.LargeChange = NoteView.TailTick - NoteView.HeadTick;
                NoteViewScrollBar.Maximum = NoteViewScrollBar.LargeChange;
            };

            NoteViewScrollBar.ValueChanged += (s, e) =>
            {
                NoteView.HeadTick = -NoteViewScrollBar.Value / 60 * 60; // 60の倍数できれいに表示されるので…
                NoteView.Invalidate();
            };

            NoteViewScrollBar.Scroll += (s, e) =>
            {
                if (e.Type == ScrollEventType.EndScroll)
                {
                    if (NoteViewScrollBar.Value < NoteViewScrollBar.Minimum / 1.2f)
                    {
                        NoteViewScrollBar.Minimum = (int)(NoteViewScrollBar.Minimum * 1.5);
                    }
                }
            };

            FormClosing += (s, e) =>
            {
                if (MessageBox.Show(this, "終了してよろしいですか？", "確認", MessageBoxButtons.YesNo) != DialogResult.Yes) e.Cancel = true;
            };

            using (var manager = this.WorkWithLayout())
            {
                this.Menu = CreateMainMenu(NoteView);
                this.Controls.Add(NoteView);
                this.Controls.Add(NoteViewScrollBar);
                this.Controls.Add(CreateNewNoteTypeToolStrip(NoteView));
                this.Controls.Add(CreateMainToolStrip(NoteView));
            }

            NoteView.NewNoteType = NoteType.Tap;
            NoteView.EditMode = EditMode.Edit;

            LoadBook(new ScoreBook());
        }

        protected void LoadBook(ScoreBook book)
        {
            ScoreBook = book;
            NoteView.Load(book.Score.Notes);
            NoteViewScrollBar.Value = 0;
            NoteViewScrollBar.Minimum = -Math.Max(NoteView.UnitBeatTick * 4 * 20, NoteView.Notes.GetLastTick());
            SetText(book.Path);
        }

        protected void LoadFile()
        {
            var dialog = new OpenFileDialog()
            {
                Filter = FileTypeFilter
            };

            if (dialog.ShowDialog(this) == DialogResult.OK)
            {
                try
                {
                    LoadBook(ScoreBook.LoadFile(dialog.FileName));
                }
                catch (Exception ex)
                {
                    MessageBox.Show("ファイルの読み込み中にエラーが発生しました。");
                    Program.DumpException(ex);
                    LoadBook(new ScoreBook());
                }
            }
        }

        protected void SaveAs()
        {
            var dialog = new SaveFileDialog()
            {
                Filter = FileTypeFilter
            };

            if (dialog.ShowDialog(this) == DialogResult.OK)
            {
                ScoreBook.Path = dialog.FileName;
                SaveFile();
                SetText(ScoreBook.Path);
            }
        }

        protected void SaveFile()
        {
            if (string.IsNullOrEmpty(ScoreBook.Path))
            {
                SaveAs();
                return;
            }
            CommitChanges();
            ScoreBook.Save();
        }

        protected void ExportFile()
        {
            CommitChanges();
            var dialog = new ExportForm(ScoreBook);
            dialog.ShowDialog(this);
        }

        protected void CommitChanges()
        {
            ScoreBook.Score.Notes = new NoteCollection(NoteView.Notes);
        }

        protected void ClearFile()
        {
            if (MessageBox.Show(this, "編集中のデータは破棄されますがよろしいですか？", "確認", MessageBoxButtons.YesNo) == DialogResult.Yes)
            {
                LoadBook(new ScoreBook());
            }
        }

        protected void SetText()
        {
            SetText(null);
        }

        protected void SetText(string filePath)
        {
            Text = (string.IsNullOrEmpty(filePath) ? "" : Path.GetFileName(filePath) + " - ") + "Ched";
        }

        private MainMenu CreateMainMenu(NoteView noteView)
        {
            var fileMenuItems = new MenuItem[]
            {
                new MenuItem("新規作成(&N)", (s, e) => ClearFile()) { Shortcut = Shortcut.CtrlN },
                new MenuItem("開く(&O)", (s, e) => LoadFile()) { Shortcut = Shortcut.CtrlO },
                new MenuItem("上書き保存(&S)", (s, e) => SaveFile()) { Shortcut = Shortcut.CtrlS },
                new MenuItem("名前を付けて保存(&A)", (s, e) => SaveAs()) { Shortcut = Shortcut.CtrlShiftS },
                new MenuItem("エクスポート", (s, e) => ExportFile()),
                new MenuItem("-"),
                new MenuItem("終了(&X)", (s, e) => this.Close())
            };

            var undoItem = new MenuItem("元に戻す", (s, e) => noteView.Undo())
            {
                Shortcut = Shortcut.CtrlZ,
                Enabled = false
            };
            var redoItem = new MenuItem("やり直し", (s, e) => noteView.Redo())
            {
                Shortcut = Shortcut.CtrlY,
                Enabled = false
            };
            var editMenuItems = new MenuItem[] { undoItem, redoItem };

            var helpMenuItems = new MenuItem[]
            {
                new MenuItem("公式サイトを開く", (s, e) => System.Diagnostics.Process.Start("https://github.com/paralleltree/Ched")),
                new MenuItem("バージョン情報", (s, e) => new VersionInfoForm().ShowDialog(this))
            };

            noteView.OperationHistoryChanged += (s, e) =>
            {
                redoItem.Enabled = noteView.CanRedo;
                undoItem.Enabled = noteView.CanUndo;
            };

            return new MainMenu(new MenuItem[]
            {
                new MenuItem("ファイル(&F)", fileMenuItems),
                new MenuItem("編集(&E)", editMenuItems),
                new MenuItem("ヘルプ(&H)", helpMenuItems)
            });
        }

        private ToolStrip CreateMainToolStrip(NoteView noteView)
        {
            var newFileButton = new ToolStripButton("新規作成", Resources.NewFileIcon, (s, e) => ClearFile())
            {
                DisplayStyle = ToolStripItemDisplayStyle.Image
            };
            var openFileButton = new ToolStripButton("開く", Resources.OpenFileIcon, (s, e) => LoadFile())
            {
                DisplayStyle = ToolStripItemDisplayStyle.Image
            };
            var saveFileButton = new ToolStripButton("上書き保存", Resources.SaveFileIcon, (s, e) => SaveFile())
            {
                DisplayStyle = ToolStripItemDisplayStyle.Image
            };
            var exportButton = new ToolStripButton("エクスポート", Resources.ExportIcon, (s, e) => ExportFile())
            {
                DisplayStyle = ToolStripItemDisplayStyle.Image
            };

            var undoButton = new ToolStripButton("元に戻す", Resources.UndoIcon, (s, e) => noteView.Undo())
            {
                DisplayStyle = ToolStripItemDisplayStyle.Image,
                Enabled = false
            };
            var redoButton = new ToolStripButton("やり直す", Resources.RedoIcon, (s, e) => noteView.Redo())
            {
                DisplayStyle = ToolStripItemDisplayStyle.Image,
                Enabled = false
            };

            var penButton = new ToolStripButton("ペン", Resources.EditIcon, (s, e) => noteView.EditMode = EditMode.Edit)
            {
                DisplayStyle = ToolStripItemDisplayStyle.Image
            };
            var eraserButton = new ToolStripButton("消しゴム", Resources.EraserIcon, (s, e) => noteView.EditMode = EditMode.Erase)
            {
                DisplayStyle = ToolStripItemDisplayStyle.Image
            };

            noteView.OperationHistoryChanged += (s, e) =>
            {
                undoButton.Enabled = noteView.CanUndo;
                redoButton.Enabled = noteView.CanRedo;
            };

            noteView.EditModeChanged += (s, e) =>
            {
                penButton.Checked = noteView.EditMode == EditMode.Edit;
                eraserButton.Checked = noteView.EditMode == EditMode.Erase;
            };

            return new ToolStrip(new ToolStripItem[]
            {
                newFileButton, openFileButton, saveFileButton, new ToolStripSeparator(),
                undoButton, redoButton, new ToolStripSeparator(),
                penButton, eraserButton
            });
        }

        private ToolStrip CreateNewNoteTypeToolStrip(NoteView noteView)
        {
            var tapButton = new ToolStripButton("TAP", Resources.TapIcon, (s, e) => noteView.NewNoteType = NoteType.Tap)
            {
                DisplayStyle = ToolStripItemDisplayStyle.Image
            };
            var exTapButton = new ToolStripButton("ExTAP", Resources.ExTapIcon, (s, e) => noteView.NewNoteType = NoteType.ExTap)
            {
                DisplayStyle = ToolStripItemDisplayStyle.Image
            };
            var holdButton = new ToolStripButton("HOLD", Resources.HoldIcon, (s, e) => noteView.NewNoteType = NoteType.Hold)
            {
                DisplayStyle = ToolStripItemDisplayStyle.Image
            };
            var slideButton = new ToolStripButton("SLIDE", Resources.SlideIcon, (s, e) =>
            {
                noteView.NewNoteType = NoteType.Slide;
                noteView.IsNewSlideStepVisible = false;
            })
            {
                DisplayStyle = ToolStripItemDisplayStyle.Image
            };
            var slideStepButton = new ToolStripButton("SLIDE(中継点)", Resources.SlideStepIcon, (s, e) =>
            {
                noteView.NewNoteType = NoteType.Slide;
                noteView.IsNewSlideStepVisible = true;
            })
            {
                DisplayStyle = ToolStripItemDisplayStyle.Image
            };
            var airActionButton = new ToolStripButton("AIR-ACTION", Resources.AirActionIcon, (s, e) => noteView.NewNoteType = NoteType.AirAction)
            {
                DisplayStyle = ToolStripItemDisplayStyle.Image
            };
            var flickButton = new ToolStripButton("FLICK", Resources.FlickIcon, (s, e) => noteView.NewNoteType = NoteType.Flick)
            {
                DisplayStyle = ToolStripItemDisplayStyle.Image
            };
            var damageButton = new ToolStripButton("DAMAGE", Resources.DamgeIcon, (s, e) => noteView.NewNoteType = NoteType.Damage)
            {
                DisplayStyle = ToolStripItemDisplayStyle.Image
            };

            var airKind = new CheckableToolStripSplitButton()
            {
                DisplayStyle = ToolStripItemDisplayStyle.Image
            };
            airKind.Text = "AIR";
            airKind.Click += (s, e) => noteView.NewNoteType = NoteType.Air;
            airKind.DropDown.Items.AddRange(new ToolStripItem[]
            {
                new ToolStripMenuItem("振り上げAIR", Resources.AirUpIcon, (s, e) => noteView.AirDirection = new AirDirection(VerticalAirDirection.Up, HorizontalAirDirection.Center)),
                new ToolStripMenuItem("振り上げ左AIR", Resources.AirLeftUpIcon, (s, e) => noteView.AirDirection = new AirDirection(VerticalAirDirection.Up, HorizontalAirDirection.Left)),
                new ToolStripMenuItem("振り上げ右AIR", Resources.AirRightUpIcon, (s, e) => noteView.AirDirection = new AirDirection(VerticalAirDirection.Up, HorizontalAirDirection.Right)),
                new ToolStripMenuItem("振り下げAIR", Resources.AirDownIcon, (s, e) => noteView.AirDirection = new AirDirection(VerticalAirDirection.Down, HorizontalAirDirection.Center)),
                new ToolStripMenuItem("振り下げ左AIR", Resources.AirLeftDownIcon, (s, e) => noteView.AirDirection = new AirDirection(VerticalAirDirection.Down, HorizontalAirDirection.Left)),
                new ToolStripMenuItem("振り下げ右AIR", Resources.AirRightDownIcon, (s, e) => noteView.AirDirection = new AirDirection(VerticalAirDirection.Down, HorizontalAirDirection.Right))
            });
            airKind.Image = Resources.AirUpIcon;

            var quantizeTicks = new int[]
            {
                4, 8, 12, 16, 24, 32, 48, 64, 96, 128, 192
            };
            var quantizeComboBox = new ToolStripComboBox("クォンタイズ")
            {
                DropDownStyle = ComboBoxStyle.DropDownList,
                AutoSize = false,
                Width = 80
            };
            quantizeComboBox.Items.AddRange(quantizeTicks.Select(p => p + "分").ToArray());
            quantizeComboBox.SelectedIndexChanged += (s, e) =>
            {
                noteView.QuantizeTick = noteView.UnitBeatTick * 4 / quantizeTicks[quantizeComboBox.SelectedIndex];
                NoteView.Focus();
            };
            quantizeComboBox.SelectedIndex = 1;

            noteView.NewNoteTypeChanged += (s, e) =>
            {
                tapButton.Checked = noteView.NewNoteType.HasFlag(NoteType.Tap);
                exTapButton.Checked = noteView.NewNoteType.HasFlag(NoteType.ExTap);
                holdButton.Checked = noteView.NewNoteType.HasFlag(NoteType.Hold);
                slideButton.Checked = noteView.NewNoteType.HasFlag(NoteType.Slide) && !noteView.IsNewSlideStepVisible;
                slideStepButton.Checked = noteView.NewNoteType.HasFlag(NoteType.Slide) && noteView.IsNewSlideStepVisible;
                airKind.Checked = noteView.NewNoteType.HasFlag(NoteType.Air);
                airActionButton.Checked = noteView.NewNoteType.HasFlag(NoteType.AirAction);
                flickButton.Checked = noteView.NewNoteType.HasFlag(NoteType.Flick);
                damageButton.Checked = noteView.NewNoteType.HasFlag(NoteType.Damage);
            };

            noteView.AirDirectionChanged += (s, e) =>
            {
                switch (noteView.AirDirection.HorizontalDirection)
                {
                    case HorizontalAirDirection.Center:
                        airKind.Image = noteView.AirDirection.VerticalDirection == VerticalAirDirection.Up ? Resources.AirUpIcon : Resources.AirDownIcon;
                        break;

                    case HorizontalAirDirection.Left:
                        airKind.Image = noteView.AirDirection.VerticalDirection == VerticalAirDirection.Up ? Resources.AirLeftUpIcon : Resources.AirLeftDownIcon;
                        break;

                    case HorizontalAirDirection.Right:
                        airKind.Image = noteView.AirDirection.VerticalDirection == VerticalAirDirection.Up ? Resources.AirRightUpIcon : Resources.AirRightDownIcon;
                        break;
                }
            };

            return new ToolStrip(new ToolStripItem[]
            {
                tapButton, exTapButton, holdButton, slideButton, slideStepButton, airKind, airActionButton, flickButton, damageButton,
                quantizeComboBox
            });
        }
    }
}
