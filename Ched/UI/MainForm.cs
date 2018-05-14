﻿using System;
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
using Ched.Components.Events;
using Ched.UI.Operations;
using Ched.Properties;

namespace Ched.UI
{
    public partial class MainForm : Form
    {
        private readonly string FileExtension = ".chs";
        private readonly string FileTypeFilter = "Ched専用形式(*.chs)|*.chs";

        private bool isPreviewMode;
        private string outputPath = "";

        private ScoreBook ScoreBook { get; set; }
        private OperationManager OperationManager { get; }

        private ScrollBar NoteViewScrollBar { get; }
        private NoteView NoteView { get; }

        private ToolStripButton ZoomInButton;
        private ToolStripButton ZoomOutButton;

        private SoundPreviewManager PreviewManager { get; }
        private SoundSource CurrentMusicSource;

        private bool IsPreviewMode
        {
            get { return isPreviewMode; }
            set
            {
                isPreviewMode = value;
                NoteView.Editable = CanEdit;
                NoteView.LaneBorderLightColor = isPreviewMode ? Color.FromArgb(40, 40, 40) : Color.FromArgb(60, 60, 60);
                NoteView.LaneBorderDarkColor = isPreviewMode ? Color.FromArgb(10, 10, 10) : Color.FromArgb(30, 30, 30);
                NoteView.UnitLaneWidth = isPreviewMode ? 4 : 12;
                NoteView.ShortNoteHeight = isPreviewMode ? 4 : 5;
                NoteView.UnitBeatHeight = isPreviewMode ? 48 : Settings.Default.UnitBeatHeight;
                UpdateThumbHeight();
                ZoomInButton.Enabled = CanZoomIn;
                ZoomOutButton.Enabled = CanZoomOut;
            }
        }

        private bool CanZoomIn { get { return !IsPreviewMode && NoteView.UnitBeatHeight < 960; } }
        private bool CanZoomOut { get { return !IsPreviewMode && NoteView.UnitBeatHeight > 30; } }
        private bool CanEdit { get { return !IsPreviewMode && !PreviewManager.Playing; } }

        public MainForm()
        {
            InitializeComponent();
            Size = new Size(420, 700);
            Icon = Resources.MainIcon;

            ToolStripManager.RenderMode = ToolStripManagerRenderMode.System;

            OperationManager = new OperationManager();
            OperationManager.OperationHistoryChanged += (s, e) => SetText(ScoreBook.Path);
            OperationManager.ChangesCommited += (s, e) => SetText(ScoreBook.Path);

            NoteView = new NoteView(OperationManager)
            {
                Dock = DockStyle.Fill,
                UnitBeatHeight = Settings.Default.UnitBeatHeight
            };

            PreviewManager = new SoundPreviewManager(NoteView);
            PreviewManager.Finished += (s, e) => NoteView.Editable = CanEdit;
            PreviewManager.TickUpdated += (s, e) => NoteView.CurrentTick = e.Tick;

            NoteViewScrollBar = new VScrollBar()
            {
                Dock = DockStyle.Right,
                Minimum = -NoteView.UnitBeatTick * 4 * 20,
                SmallChange = NoteView.UnitBeatTick
            };

            Action<ScrollBar> processScrollBarRangeExtension = s =>
            {
                if (NoteViewScrollBar.Value < NoteViewScrollBar.Minimum * 0.9f)
                {
                    NoteViewScrollBar.Minimum = (int)(NoteViewScrollBar.Minimum * 1.2);
                }
            };

            NoteView.Resize += (s, e) => UpdateThumbHeight();

            NoteView.MouseWheel += (s, e) =>
            {
                int value = NoteViewScrollBar.Value - e.Delta / 120 * NoteViewScrollBar.SmallChange;
                NoteViewScrollBar.Value = Math.Min(Math.Max(value, NoteViewScrollBar.Minimum), NoteViewScrollBar.GetMaximumValue());
                processScrollBarRangeExtension(NoteViewScrollBar);
            };

            NoteView.DragScroll += (s, e) =>
            {
                NoteViewScrollBar.Value = Math.Max(-NoteView.HeadTick, NoteViewScrollBar.Minimum);
                processScrollBarRangeExtension(NoteViewScrollBar);
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
                    processScrollBarRangeExtension(NoteViewScrollBar);
                }
            };

            AllowDrop = true;
            DragEnter += (s, e) =>
            {
                e.Effect = DragDropEffects.None;
                if (e.Data.GetDataPresent(DataFormats.FileDrop))
                {
                    var items = (string[])e.Data.GetData(DataFormats.FileDrop);
                    if (items.Length == 1 && items.All(p => Path.GetExtension(p) == FileExtension && File.Exists(p)))
                        e.Effect = DragDropEffects.Copy;
                }
            };
            DragDrop += (s, e) =>
            {
                string path = ((string[])e.Data.GetData(DataFormats.FileDrop)).Single();
                if (OperationManager.IsChanged && !this.ConfirmDiscardChanges()) return;
                LoadFile(path);
            };

            FormClosing += (s, e) =>
            {
                if (OperationManager.IsChanged && !this.ConfirmDiscardChanges())
                {
                    e.Cancel = true;
                    return;
                }

                Settings.Default.Save();
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
            SetText();
        }

        public MainForm(string filePath) : this()
        {
            LoadFile(filePath);
        }

        protected void LoadFile(string filePath)
        {
            try
            {
                if (!ScoreBook.IsCompatible(filePath))
                {
                    MessageBox.Show(this, "現在のバージョンでは開けないファイルです。", Program.ApplicationName, MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }
                if (!ScoreBook.IsUpgradeNeeded(filePath))
                {
                    if (MessageBox.Show(this, "古いバージョンで作成されたファイルです。\nバージョンアップしてよろしいですか？\n(以前のバージョンでは開けなくなります。)", Program.ApplicationName, MessageBoxButtons.YesNo, MessageBoxIcon.Warning) == DialogResult.No)
                        return;
                }
                LoadBook(ScoreBook.LoadFile(filePath));
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, "ファイルの読み込み中にエラーが発生しました。", Program.ApplicationName, MessageBoxButtons.OK, MessageBoxIcon.Error);
                Program.DumpException(ex);
                LoadBook(new ScoreBook());
            }
        }

        protected void LoadBook(ScoreBook book)
        {
            ScoreBook = book;
            NoteView.LoadScore(book.Score);
            NoteViewScrollBar.Value = NoteViewScrollBar.GetMaximumValue();
            NoteViewScrollBar.Minimum = -Math.Max(NoteView.UnitBeatTick * 4 * 20, NoteView.Notes.GetLastTick());
            UpdateThumbHeight();
            SetText(book.Path);
            if (!string.IsNullOrEmpty(book.Path))
            {
                SoundConfiguration.Default.ScoreSound.TryGetValue(book.Path, out CurrentMusicSource);
            }
            else
            {
                CurrentMusicSource = null;
            }
        }

        protected void OpenFile()
        {
            if (OperationManager.IsChanged && !this.ConfirmDiscardChanges()) return;

            var dialog = new OpenFileDialog()
            {
                Filter = FileTypeFilter
            };

            if (dialog.ShowDialog(this) == DialogResult.OK)
            {
                LoadFile(dialog.FileName);
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
            if (CurrentMusicSource != null)
            {
                SoundConfiguration.Default.ScoreSound[ScoreBook.Path] = CurrentMusicSource;
                SoundConfiguration.Default.Save();
            }
            OperationManager.CommitChanges();
        }

        protected void ExportFile()
        {
            CommitChanges();
            var dialog = new ExportForm(ScoreBook, outputPath);
            dialog.ShowDialog(this);
            outputPath = dialog.OutputPath;
        }

        protected void CommitChanges()
        {
            ScoreBook.Score.Notes = new NoteCollection(NoteView.Notes);
            // Eventsは参照渡ししてますよん
        }

        protected void ClearFile()
        {
            if (!OperationManager.IsChanged || this.ConfirmDiscardChanges())
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
            Text = "Ched" + (string.IsNullOrEmpty(filePath) ? "" : " - " + Path.GetFileName(filePath)) + (OperationManager.IsChanged ? " *" : "");
        }

        private void UpdateThumbHeight()
        {
            NoteViewScrollBar.LargeChange = NoteView.TailTick - NoteView.HeadTick;
            NoteViewScrollBar.Maximum = NoteViewScrollBar.LargeChange + NoteView.PaddingHeadTick;
        }

        private MainMenu CreateMainMenu(NoteView noteView)
        {
            var bookPropertiesMenuItem = new MenuItem("譜面プロパティ", (s, e) =>
            {
                var form = new BookPropertiesForm(ScoreBook, CurrentMusicSource);
                if (form.ShowDialog(this) == DialogResult.OK)
                {
                    CurrentMusicSource = form.MusicSource;
                    if (string.IsNullOrEmpty(ScoreBook.Path)) return;
                    SoundConfiguration.Default.ScoreSound[ScoreBook.Path] = CurrentMusicSource;
                    SoundConfiguration.Default.Save();
                }
            });

            var fileMenuItems = new MenuItem[]
            {
                new MenuItem("新規作成(&N)", (s, e) => ClearFile()) { Shortcut = Shortcut.CtrlN },
                new MenuItem("開く(&O)", (s, e) => OpenFile()) { Shortcut = Shortcut.CtrlO },
                new MenuItem("上書き保存(&S)", (s, e) => SaveFile()) { Shortcut = Shortcut.CtrlS },
                new MenuItem("名前を付けて保存(&A)", (s, e) => SaveAs()) { Shortcut = Shortcut.CtrlShiftS },
                new MenuItem("エクスポート", (s, e) => ExportFile()),
                new MenuItem("-"),
                bookPropertiesMenuItem,
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

            var copyItem = new MenuItem("コピー", (s, e) => noteView.CopySelectedNotes(), Shortcut.CtrlC);
            var pasteItem = new MenuItem("貼り付け", (s, e) => noteView.PasteNotes(), Shortcut.CtrlV);

            var flipSelectedNotesItem = new MenuItem("選択範囲内ノーツを反転", (s, e) => NoteView.FlipSelectedNotes());

            var removeEventsItem = new MenuItem("選択範囲内のイベントを削除", (s, e) =>
            {
                int minTick = noteView.SelectedRange.StartTick + (noteView.SelectedRange.Duration < 0 ? noteView.SelectedRange.Duration : 0);
                int maxTick = noteView.SelectedRange.StartTick + (noteView.SelectedRange.Duration < 0 ? 0 : noteView.SelectedRange.Duration);
                Func<EventBase, bool> isContained = p => p.Tick != 0 && minTick <= p.Tick && maxTick >= p.Tick;
                var events = ScoreBook.Score.Events;

                var bpmOp = events.BPMChangeEvents.Where(p => isContained(p)).ToList().Select(p =>
                {
                    ScoreBook.Score.Events.BPMChangeEvents.Remove(p);
                    return new RemoveEventOperation<BPMChangeEvent>(events.BPMChangeEvents, p);
                }).ToList();

                var speedOp = events.HighSpeedChangeEvents.Where(p => isContained(p)).ToList().Select(p =>
                {
                    ScoreBook.Score.Events.HighSpeedChangeEvents.Remove(p);
                    return new RemoveEventOperation<HighSpeedChangeEvent>(events.HighSpeedChangeEvents, p);
                }).ToList();

                var signatureOp = events.TimeSignatureChangeEvents.Where(p => isContained(p)).ToList().Select(p =>
                {
                    ScoreBook.Score.Events.TimeSignatureChangeEvents.Remove(p);
                    return new RemoveEventOperation<TimeSignatureChangeEvent>(events.TimeSignatureChangeEvents, p);
                }).ToList();

                OperationManager.Push(new CompositeOperation("イベント削除", bpmOp.Cast<IOperation>().Concat(speedOp).Concat(signatureOp)));
                noteView.Invalidate();
            });

            var editMenuItems = new MenuItem[]
            {
                undoItem, redoItem, new MenuItem("-"),
                copyItem, pasteItem, new MenuItem("-"),
                flipSelectedNotesItem, removeEventsItem
            };

            var viewModeItem = new MenuItem("譜面プレビュー", (s, e) =>
            {
                IsPreviewMode = !IsPreviewMode;
                ((MenuItem)s).Checked = IsPreviewMode;
            });

            var viewMenuItems = new MenuItem[] { viewModeItem };

            var insertBPMItem = new MenuItem("BPM", (s, e) =>
            {
                var form = new BPMSelectionForm();
                if (form.ShowDialog(this) != DialogResult.OK) return;

                var prev = noteView.ScoreEvents.BPMChangeEvents.SingleOrDefault(p => p.Tick == noteView.SelectedRange.StartTick);
                var item = new Components.Events.BPMChangeEvent()
                {
                    Tick = noteView.SelectedRange.StartTick,
                    BPM = form.BPM
                };

                var insertOp = new InsertEventOperation<Components.Events.BPMChangeEvent>(noteView.ScoreEvents.BPMChangeEvents, item);
                if (prev == null)
                {
                    OperationManager.Push(insertOp);
                }
                else
                {
                    var removeOp = new RemoveEventOperation<Components.Events.BPMChangeEvent>(noteView.ScoreEvents.BPMChangeEvents, prev);
                    noteView.ScoreEvents.BPMChangeEvents.Remove(prev);
                    OperationManager.Push(new CompositeOperation(insertOp.Description, new IOperation[] { removeOp, insertOp }));
                }

                noteView.ScoreEvents.BPMChangeEvents.Add(item);
                noteView.Invalidate();
            });

            var insertHighSpeedItem = new MenuItem("ハイスピード", (s, e) =>
            {
                var form = new HighSpeedSelectionForm();
                if (form.ShowDialog(this) != DialogResult.OK) return;

                var prev = noteView.ScoreEvents.HighSpeedChangeEvents.SingleOrDefault(p => p.Tick == noteView.SelectedRange.StartTick);
                var item = new Components.Events.HighSpeedChangeEvent()
                {
                    Tick = noteView.SelectedRange.StartTick,
                    SpeedRatio = form.SpeedRatio
                };

                var insertOp = new InsertEventOperation<Components.Events.HighSpeedChangeEvent>(noteView.ScoreEvents.HighSpeedChangeEvents, item);
                if (prev == null)
                {
                    OperationManager.Push(insertOp);
                }
                else
                {
                    var removeOp = new RemoveEventOperation<Components.Events.HighSpeedChangeEvent>(NoteView.ScoreEvents.HighSpeedChangeEvents, prev);
                    noteView.ScoreEvents.HighSpeedChangeEvents.Remove(prev);
                    OperationManager.Push(new CompositeOperation(insertOp.Description, new IOperation[] { removeOp, insertOp }));
                }

                noteView.ScoreEvents.HighSpeedChangeEvents.Add(item);
                noteView.Invalidate();
            });

            var insertTimeSignatureItem = new MenuItem("拍子", (s, e) =>
            {
                var form = new TimeSignatureSelectionForm();
                if (form.ShowDialog(this) != DialogResult.OK) return;

                var prev = noteView.ScoreEvents.TimeSignatureChangeEvents.SingleOrDefault(p => p.Tick == noteView.SelectedRange.StartTick);
                var item = new Components.Events.TimeSignatureChangeEvent()
                {
                    Tick = noteView.SelectedRange.StartTick,
                    Numerator = form.Numerator,
                    DenominatorExponent = form.DenominatorExponent
                };

                var insertOp = new InsertEventOperation<Components.Events.TimeSignatureChangeEvent>(noteView.ScoreEvents.TimeSignatureChangeEvents, item);
                if (prev != null)
                {
                    noteView.ScoreEvents.TimeSignatureChangeEvents.Remove(prev);
                    var removeOp = new RemoveEventOperation<Components.Events.TimeSignatureChangeEvent>(noteView.ScoreEvents.TimeSignatureChangeEvents, prev);
                    OperationManager.Push(new CompositeOperation(insertOp.Description, new IOperation[] { removeOp, insertOp }));
                }
                else
                {
                    OperationManager.Push(insertOp);
                }

                noteView.ScoreEvents.TimeSignatureChangeEvents.Add(item);
                noteView.Invalidate();
            });

            var insertMenuItems = new MenuItem[] { insertBPMItem, insertHighSpeedItem, insertTimeSignatureItem };

            var playItem = new MenuItem("再生/停止", (s, e) =>
            {
                if (CurrentMusicSource == null)
                {
                    MessageBox.Show(this, "譜面プロパティから音源ファイルを指定してください。", "エラー", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }
                if (!File.Exists(CurrentMusicSource.FilePath))
                {
                    MessageBox.Show(this, "音源ファイルが見つかりません。", "エラー", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                if (PreviewManager.Playing)
                {
                    PreviewManager.Stop();
                    return;
                }

                int startTick = noteView.CurrentTick;
                EventHandler lambda = null;
                lambda = (p, q) =>
                {
                    PreviewManager.Finished -= lambda;
                    noteView.CurrentTick = startTick;
                };

                if (PreviewManager.Start(CurrentMusicSource, startTick))
                {
                    PreviewManager.Finished += lambda;
                    NoteView.Editable = CanEdit;
                }
            });

            var stopItem = new MenuItem("停止", (s, e) =>
            {
                PreviewManager.Stop();
            });

            var playMenuItems = new MenuItem[]
            {
                playItem, stopItem
            };

            var helpMenuItems = new MenuItem[]
            {
                new MenuItem("プロジェクトサイトを開く", (s, e) => System.Diagnostics.Process.Start("https://github.com/paralleltree/Ched")),
                new MenuItem("バージョン情報", (s, e) => new VersionInfoForm().ShowDialog(this))
            };

            OperationManager.OperationHistoryChanged += (s, e) =>
            {
                redoItem.Enabled = noteView.CanRedo;
                undoItem.Enabled = noteView.CanUndo;
            };

            return new MainMenu(new MenuItem[]
            {
                new MenuItem("ファイル(&F)", fileMenuItems),
                new MenuItem("編集(&E)", editMenuItems),
                new MenuItem("表示(&V)", viewMenuItems),
                new MenuItem("挿入(&I)", insertMenuItems),
                new MenuItem("再生(&P)", playMenuItems),
                new MenuItem("ヘルプ(&H)", helpMenuItems)
            });
        }

        private ToolStrip CreateMainToolStrip(NoteView noteView)
        {
            var newFileButton = new ToolStripButton("新規作成", Resources.NewFileIcon, (s, e) => ClearFile())
            {
                DisplayStyle = ToolStripItemDisplayStyle.Image
            };
            var openFileButton = new ToolStripButton("開く", Resources.OpenFileIcon, (s, e) => OpenFile())
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

            var copyButton = new ToolStripButton("コピー", Resources.CopyIcon, (s, e) => noteView.CopySelectedNotes())
            {
                DisplayStyle = ToolStripItemDisplayStyle.Image
            };
            var pasteButton = new ToolStripButton("貼り付け", Resources.PasteIcon, (s, e) => noteView.PasteNotes())
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
            var selectionButton = new ToolStripButton("選択", Resources.SelectionIcon, (s, e) => noteView.EditMode = EditMode.Select)
            {
                DisplayStyle = ToolStripItemDisplayStyle.Image
            };
            var eraserButton = new ToolStripButton("消しゴム", Resources.EraserIcon, (s, e) => noteView.EditMode = EditMode.Erase)
            {
                DisplayStyle = ToolStripItemDisplayStyle.Image
            };

            var zoomInButton = new ToolStripButton("拡大", Resources.ZoomInIcon)
            {
                Enabled = noteView.UnitBeatHeight < 960,
                DisplayStyle = ToolStripItemDisplayStyle.Image
            };
            var zoomOutButton = new ToolStripButton("縮小", Resources.ZoomOutIcon)
            {
                Enabled = noteView.UnitBeatHeight > 30,
                DisplayStyle = ToolStripItemDisplayStyle.Image
            };

            zoomInButton.Click += (s, e) =>
            {
                noteView.UnitBeatHeight *= 2;
                Settings.Default.UnitBeatHeight = (int)noteView.UnitBeatHeight;
                zoomOutButton.Enabled = CanZoomOut;
                zoomInButton.Enabled = CanZoomIn;
                UpdateThumbHeight();
            };

            zoomOutButton.Click += (s, e) =>
            {
                noteView.UnitBeatHeight /= 2;
                Settings.Default.UnitBeatHeight = (int)noteView.UnitBeatHeight;
                zoomInButton.Enabled = CanZoomIn;
                zoomOutButton.Enabled = CanZoomOut;
                UpdateThumbHeight();
            };

            ZoomInButton = zoomInButton;
            ZoomOutButton = zoomOutButton;

            OperationManager.OperationHistoryChanged += (s, e) =>
            {
                undoButton.Enabled = noteView.CanUndo;
                redoButton.Enabled = noteView.CanRedo;
            };

            noteView.EditModeChanged += (s, e) =>
            {
                selectionButton.Checked = noteView.EditMode == EditMode.Select;
                penButton.Checked = noteView.EditMode == EditMode.Edit;
                eraserButton.Checked = noteView.EditMode == EditMode.Erase;
            };

            return new ToolStrip(new ToolStripItem[]
            {
                newFileButton, openFileButton, saveFileButton, exportButton, new ToolStripSeparator(),
                copyButton, pasteButton, new ToolStripSeparator(),
                undoButton, redoButton, new ToolStripSeparator(),
                penButton, selectionButton, eraserButton, new ToolStripSeparator(),
                zoomInButton, zoomOutButton
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
            quantizeComboBox.Items.Add("カスタム");
            quantizeComboBox.SelectedIndexChanged += (s, e) =>
            {
                if (quantizeComboBox.SelectedIndex == quantizeComboBox.Items.Count - 1)
                {
                    // ユーザー定義
                    var form = new CustomQuantizeSelectionForm(ScoreBook.Score.TicksPerBeat * 4);
                    if (form.ShowDialog(this) == DialogResult.OK)
                    {
                        noteView.QuantizeTick = form.QuantizeTick;
                    }
                }
                else
                {
                    noteView.QuantizeTick = noteView.UnitBeatTick * 4 / quantizeTicks[quantizeComboBox.SelectedIndex];
                }
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
