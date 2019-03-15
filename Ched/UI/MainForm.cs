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

using Ched.Core.Notes;
using Ched.Core;
using Ched.Core.Events;
using Ched.Configuration;
using Ched.Localization;
using Ched.Plugins;
using Ched.Properties;
using Ched.UI.Operations;

namespace Ched.UI
{
    public partial class MainForm : Form
    {
        private readonly string FileExtension = ".chs";
        private string FileTypeFilter => FileFilterStrings.ChedFilter + string.Format("({0})|{1}", "*" + FileExtension, "*" + FileExtension);

        private bool isPreviewMode;

        private ScoreBook ScoreBook { get; set; }
        private OperationManager OperationManager { get; }

        private ScrollBar NoteViewScrollBar { get; }
        private NoteView NoteView { get; }

        private ToolStripButton ZoomInButton;
        private ToolStripButton ZoomOutButton;

        private ExportData LastExportData { get; set; }

        private SoundPreviewManager PreviewManager { get; }
        private SoundSource CurrentMusicSource;

        private Plugins.PluginManager PluginManager { get; } = Plugins.PluginManager.GetInstance();

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
                NoteView.UnitBeatHeight = isPreviewMode ? 48 : ApplicationSettings.Default.UnitBeatHeight;
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
                UnitBeatHeight = ApplicationSettings.Default.UnitBeatHeight,
                InsertAirWithAirAction = ApplicationSettings.Default.InsertAirWithAirAction
            };

            PreviewManager = new SoundPreviewManager(NoteView);
            PreviewManager.IsStopAtLastNote = ApplicationSettings.Default.IsPreviewAbortAtLastNote;
            PreviewManager.Finished += (s, e) => NoteView.Editable = CanEdit;
            PreviewManager.TickUpdated += (s, e) => NoteView.CurrentTick = e.Tick;
            PreviewManager.ExceptionThrown += (s, e) => MessageBox.Show(this, ErrorStrings.PreviewException, Program.ApplicationName, MessageBoxButtons.OK, MessageBoxIcon.Error);

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

                ApplicationSettings.Default.Save();
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

            LoadEmptyBook();
            SetText();

            if (!PreviewManager.IsSupported)
                MessageBox.Show(this, ErrorStrings.PreviewNotSupported, Program.ApplicationName, MessageBoxButtons.OK, MessageBoxIcon.Information);

            if (PluginManager.FailedFiles.Count > 0)
            {
                MessageBox.Show(this, string.Join("\n", new[] { ErrorStrings.PluginLoadError }.Concat(PluginManager.FailedFiles)), Program.ApplicationName, MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
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
                    MessageBox.Show(this, ErrorStrings.FileNotCompatible, Program.ApplicationName, MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }
                if (!ScoreBook.IsUpgradeNeeded(filePath))
                {
                    if (MessageBox.Show(this, ErrorStrings.FileUpgradeNeeded, Program.ApplicationName, MessageBoxButtons.YesNo, MessageBoxIcon.Warning) == DialogResult.No)
                        return;
                }
                LoadBook(ScoreBook.LoadFile(filePath));
            }
            catch (UnauthorizedAccessException)
            {
                MessageBox.Show(this, ErrorStrings.FileNotAccessible, Program.ApplicationName, MessageBoxButtons.OK, MessageBoxIcon.Error);
                LoadEmptyBook();
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, ErrorStrings.FileLoadError, Program.ApplicationName, MessageBoxButtons.OK, MessageBoxIcon.Error);
                Program.DumpExceptionTo(ex, "file_exception.json");
                LoadEmptyBook();
            }
        }

        protected void LoadBook(ScoreBook book)
        {
            ScoreBook = book;
            OperationManager.Clear();
            NoteView.Initialize(book.Score);
            NoteViewScrollBar.Value = NoteViewScrollBar.GetMaximumValue();
            NoteViewScrollBar.Minimum = -Math.Max(NoteView.UnitBeatTick * 4 * 20, NoteView.Notes.GetLastTick());
            NoteViewScrollBar.SmallChange = NoteView.UnitBeatTick;
            UpdateThumbHeight();
            SetText(book.Path);
            LastExportData = null;
            if (!string.IsNullOrEmpty(book.Path))
            {
                SoundSettings.Default.ScoreSound.TryGetValue(book.Path, out CurrentMusicSource);
            }
            else
            {
                CurrentMusicSource = null;
            }
        }

        protected void LoadEmptyBook()
        {
            var book = new ScoreBook();
            var events = book.Score.Events;
            events.BPMChangeEvents.Add(new BPMChangeEvent() { Tick = 0, BPM = 120 });
            events.TimeSignatureChangeEvents.Add(new TimeSignatureChangeEvent() { Tick = 0, Numerator = 4, DenominatorExponent = 2 });
            LoadBook(book);
        }

        protected void OpenFile()
        {
            OpenFile(FileTypeFilter, p => LoadFile(p));
        }

        protected void OpenFile(string filter, Action<string> loadAction)
        {
            if (OperationManager.IsChanged && !this.ConfirmDiscardChanges()) return;

            var dialog = new OpenFileDialog()
            {
                Filter = filter
            };

            if (dialog.ShowDialog(this) == DialogResult.OK)
            {
                loadAction(dialog.FileName);
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
                SoundSettings.Default.ScoreSound[ScoreBook.Path] = CurrentMusicSource;
                SoundSettings.Default.Save();
            }
            OperationManager.CommitChanges();
        }

        protected void ExportFile()
        {
            CommitChanges();
            var dialog = new ExportForm(ScoreBook);
            if (dialog.ShowDialog(this) == DialogResult.OK)
            {
                LastExportData = new ExportData() { OutputPath = dialog.OutputPath, Exporter = dialog.Exporter };
            }
        }

        protected void CommitChanges()
        {
            ScoreBook.Score.Notes = NoteView.Notes.Reposit();
            // Eventsは参照渡ししてますよん
        }

        protected void ClearFile()
        {
            if (!OperationManager.IsChanged || this.ConfirmDiscardChanges())
            {
                LoadEmptyBook();
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
            var importPluginItems = PluginManager.ScoreBookImportPlugins.Select(p => new MenuItem(p.DisplayName, (s, e) =>
            {
                OpenFile(p.FileFilter, q =>
                {
                    try
                    {
                        using (var reader = new StreamReader(q))
                            LoadBook(p.Import(reader));
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show(this, ErrorStrings.ImportFailed, Program.ApplicationName, MessageBoxButtons.OK, MessageBoxIcon.Error);
                        Program.DumpExceptionTo(ex, "import_exception.json");
                        LoadEmptyBook();
                    }
                });
            })).ToArray();

            var bookPropertiesMenuItem = new MenuItem(MainFormStrings.bookProperty, (s, e) =>
            {
                var form = new BookPropertiesForm(ScoreBook, CurrentMusicSource);
                if (form.ShowDialog(this) == DialogResult.OK)
                {
                    CurrentMusicSource = form.MusicSource;
                    if (string.IsNullOrEmpty(ScoreBook.Path)) return;
                    SoundSettings.Default.ScoreSound[ScoreBook.Path] = CurrentMusicSource;
                    SoundSettings.Default.Save();
                }
            });

            var fileMenuItems = new MenuItem[]
            {
                new MenuItem(MainFormStrings.NewFile + "(&N)", (s, e) => ClearFile()) { Shortcut = Shortcut.CtrlN },
                new MenuItem(MainFormStrings.OpenFile + "(&O)", (s, e) => OpenFile()) { Shortcut = Shortcut.CtrlO },
                new MenuItem(MainFormStrings.SaveFile + "(&S)", (s, e) => SaveFile()) { Shortcut = Shortcut.CtrlS },
                new MenuItem(MainFormStrings.SaveAs + "(&A)", (s, e) => SaveAs()) { Shortcut = Shortcut.CtrlShiftS },
                new MenuItem("-"),
                new MenuItem(MainFormStrings.Import, importPluginItems),
                new MenuItem(MainFormStrings.Export, (s, e) => ExportFile()),
                new MenuItem("-"),
                bookPropertiesMenuItem,
                new MenuItem("-"),
                new MenuItem(MainFormStrings.Exit + "(&X)", (s, e) => this.Close())
            };

            var undoItem = new MenuItem(MainFormStrings.Undo, (s, e) => noteView.Undo())
            {
                Shortcut = Shortcut.CtrlZ,
                Enabled = false
            };
            var redoItem = new MenuItem(MainFormStrings.Redo, (s, e) => noteView.Redo())
            {
                Shortcut = Shortcut.CtrlY,
                Enabled = false
            };

            var cutItem = new MenuItem(MainFormStrings.Cut, (s, e) => noteView.CutSelectedNotes(), Shortcut.CtrlX);
            var copyItem = new MenuItem(MainFormStrings.Copy, (s, e) => noteView.CopySelectedNotes(), Shortcut.CtrlC);
            var pasteItem = new MenuItem(MainFormStrings.Paste, (s, e) => noteView.PasteNotes(), Shortcut.CtrlV);
            var pasteFlippedItem = new MenuItem(MainFormStrings.PasteFlipped, (s, e) => noteView.PasteFlippedNotes(), Shortcut.CtrlShiftV);

            var flipSelectedNotesItem = new MenuItem(MainFormStrings.FlipSelectedNotes, (s, e) => noteView.FlipSelectedNotes());
            var removeSelectedNotesItem = new MenuItem(MainFormStrings.RemoveSelectedNotes, (s, e) => noteView.RemoveSelectedNotes(), Shortcut.Del);

            var removeEventsItem = new MenuItem(MainFormStrings.RemoveEvents, (s, e) =>
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

            var insertAirWithAirActionItem = new MenuItem(MainFormStrings.InsertAirWithAirAction, (s, e) =>
            {
                var item = s as MenuItem;
                item.Checked = !item.Checked;
                NoteView.InsertAirWithAirAction = item.Checked;
                ApplicationSettings.Default.InsertAirWithAirAction = item.Checked;
            })
            {
                Checked = ApplicationSettings.Default.InsertAirWithAirAction
            };

            var pluginItems = PluginManager.ScorePlugins.Select(p => new MenuItem(p.DisplayName, (s, e) =>
            {
                CommitChanges();
                Action<Score> updateScore = newScore =>
                {
                    var op = new UpdateScoreOperation(ScoreBook.Score, newScore, score =>
                    {
                        ScoreBook.Score = score;
                        noteView.UpdateScore(score);
                    });
                    OperationManager.Push(op);
                    op.Redo();
                };

                try
                {
                    p.Run(new ScorePluginArgs(() => ScoreBook.Score.Clone(), noteView.SelectedRange, updateScore));
                }
                catch (Exception ex)
                {
                    Program.DumpExceptionTo(ex, "plugin_exception.json");
                    MessageBox.Show(this, ErrorStrings.PluginException, Program.ApplicationName, MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            })).ToArray();
            var pluginItem = new MenuItem(MainFormStrings.Plugin, pluginItems);

            var editMenuItems = new MenuItem[]
            {
                undoItem, redoItem, new MenuItem("-"),
                cutItem, copyItem, pasteItem, pasteFlippedItem, new MenuItem("-"),
                flipSelectedNotesItem, removeSelectedNotesItem, removeEventsItem, new MenuItem("-"),
                insertAirWithAirActionItem, new MenuItem("-"),
                pluginItem
            };

            var viewModeItem = new MenuItem(MainFormStrings.ScorePreview, (s, e) =>
            {
                IsPreviewMode = !IsPreviewMode;
                ((MenuItem)s).Checked = IsPreviewMode;
            }, Shortcut.CtrlP);

            var viewMenuItems = new MenuItem[] { viewModeItem };

            var insertBPMItem = new MenuItem("BPM", (s, e) =>
            {
                var form = new BPMSelectionForm()
                {
                    BPM = noteView.ScoreEvents.BPMChangeEvents.OrderBy(p => p.Tick).LastOrDefault(p => p.Tick <= noteView.CurrentTick)?.BPM ?? 120m
                };
                if (form.ShowDialog(this) != DialogResult.OK) return;

                var prev = noteView.ScoreEvents.BPMChangeEvents.SingleOrDefault(p => p.Tick == noteView.SelectedRange.StartTick);
                var item = new BPMChangeEvent()
                {
                    Tick = noteView.SelectedRange.StartTick,
                    BPM = form.BPM
                };

                var insertOp = new InsertEventOperation<BPMChangeEvent>(noteView.ScoreEvents.BPMChangeEvents, item);
                if (prev == null)
                {
                    OperationManager.Push(insertOp);
                }
                else
                {
                    var removeOp = new RemoveEventOperation<BPMChangeEvent>(noteView.ScoreEvents.BPMChangeEvents, prev);
                    noteView.ScoreEvents.BPMChangeEvents.Remove(prev);
                    OperationManager.Push(new CompositeOperation(insertOp.Description, new IOperation[] { removeOp, insertOp }));
                }

                noteView.ScoreEvents.BPMChangeEvents.Add(item);
                noteView.Invalidate();
            });

            var insertHighSpeedItem = new MenuItem(MainFormStrings.HighSpeed, (s, e) =>
            {
                var form = new HighSpeedSelectionForm()
                {
                    SpeedRatio = noteView.ScoreEvents.HighSpeedChangeEvents.OrderBy(p => p.Tick).LastOrDefault(p => p.Tick <= noteView.CurrentTick)?.SpeedRatio ?? 1.0m
                };
                if (form.ShowDialog(this) != DialogResult.OK) return;

                var prev = noteView.ScoreEvents.HighSpeedChangeEvents.SingleOrDefault(p => p.Tick == noteView.SelectedRange.StartTick);
                var item = new HighSpeedChangeEvent()
                {
                    Tick = noteView.SelectedRange.StartTick,
                    SpeedRatio = form.SpeedRatio
                };

                var insertOp = new InsertEventOperation<HighSpeedChangeEvent>(noteView.ScoreEvents.HighSpeedChangeEvents, item);
                if (prev == null)
                {
                    OperationManager.Push(insertOp);
                }
                else
                {
                    var removeOp = new RemoveEventOperation<HighSpeedChangeEvent>(noteView.ScoreEvents.HighSpeedChangeEvents, prev);
                    noteView.ScoreEvents.HighSpeedChangeEvents.Remove(prev);
                    OperationManager.Push(new CompositeOperation(insertOp.Description, new IOperation[] { removeOp, insertOp }));
                }

                noteView.ScoreEvents.HighSpeedChangeEvents.Add(item);
                noteView.Invalidate();
            });

            var insertTimeSignatureItem = new MenuItem(MainFormStrings.TimeSignature, (s, e) =>
            {
                var form = new TimeSignatureSelectionForm();
                if (form.ShowDialog(this) != DialogResult.OK) return;

                var prev = noteView.ScoreEvents.TimeSignatureChangeEvents.SingleOrDefault(p => p.Tick == noteView.SelectedRange.StartTick);
                var item = new TimeSignatureChangeEvent()
                {
                    Tick = noteView.SelectedRange.StartTick,
                    Numerator = form.Numerator,
                    DenominatorExponent = form.DenominatorExponent
                };

                var insertOp = new InsertEventOperation<TimeSignatureChangeEvent>(noteView.ScoreEvents.TimeSignatureChangeEvents, item);
                if (prev != null)
                {
                    noteView.ScoreEvents.TimeSignatureChangeEvents.Remove(prev);
                    var removeOp = new RemoveEventOperation<TimeSignatureChangeEvent>(noteView.ScoreEvents.TimeSignatureChangeEvents, prev);
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

            var isAbortAtLastNoteItem = new MenuItem(MainFormStrings.AbortAtLastNote, (s, e) =>
            {
                var item = s as MenuItem;
                item.Checked = !item.Checked;
                PreviewManager.IsStopAtLastNote = item.Checked;
                ApplicationSettings.Default.IsPreviewAbortAtLastNote = item.Checked;
            })
            {
                Checked = ApplicationSettings.Default.IsPreviewAbortAtLastNote
            };

            var playItem = new MenuItem(MainFormStrings.Play, (s, e) =>
            {
                if (CurrentMusicSource == null)
                {
                    MessageBox.Show(this, ErrorStrings.MusicSourceNull, Program.ApplicationName, MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }
                if (!File.Exists(CurrentMusicSource.FilePath))
                {
                    MessageBox.Show(this, ErrorStrings.SourceFileNotFound, Program.ApplicationName, MessageBoxButtons.OK, MessageBoxIcon.Warning);
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
                    isAbortAtLastNoteItem.Enabled = true;
                    PreviewManager.Finished -= lambda;
                    noteView.CurrentTick = startTick;
                };

                if (PreviewManager.Start(CurrentMusicSource, startTick))
                {
                    isAbortAtLastNoteItem.Enabled = false;
                    PreviewManager.Finished += lambda;
                    noteView.Editable = CanEdit;
                }
            }, (Shortcut)Keys.Space);

            var stopItem = new MenuItem(MainFormStrings.Stop, (s, e) =>
            {
                PreviewManager.Stop();
            });

            var playMenuItems = new MenuItem[]
            {
                playItem, stopItem, new MenuItem("-"),
                isAbortAtLastNoteItem
            };

            var helpMenuItems = new MenuItem[]
            {
                new MenuItem(MainFormStrings.Help, (s, e) => System.Diagnostics.Process.Start("https://github.com/paralleltree/Ched/wiki"), Shortcut.F1),
                new MenuItem(MainFormStrings.VersionInfo, (s, e) => new VersionInfoForm().ShowDialog(this))
            };

            OperationManager.OperationHistoryChanged += (s, e) =>
            {
                redoItem.Enabled = noteView.CanRedo;
                undoItem.Enabled = noteView.CanUndo;
            };

            return new MainMenu(new MenuItem[]
            {
                new MenuItem(MainFormStrings.FileMenu, fileMenuItems),
                new MenuItem(MainFormStrings.EditMenu, editMenuItems),
                new MenuItem(MainFormStrings.ViewMenu, viewMenuItems),
                new MenuItem(MainFormStrings.InsertMenu, insertMenuItems),
                // PreviewManager初期化後じゃないといけないのダメ設計でしょ
                new MenuItem(MainFormStrings.PlayMenu, playMenuItems) { Enabled = PreviewManager.IsSupported },
                new MenuItem(MainFormStrings.HelpMenu, helpMenuItems)
            });
        }

        private ToolStrip CreateMainToolStrip(NoteView noteView)
        {
            var newFileButton = new ToolStripButton(MainFormStrings.NewFile, Resources.NewFileIcon, (s, e) => ClearFile())
            {
                DisplayStyle = ToolStripItemDisplayStyle.Image
            };
            var openFileButton = new ToolStripButton(MainFormStrings.OpenFile, Resources.OpenFileIcon, (s, e) => OpenFile())
            {
                DisplayStyle = ToolStripItemDisplayStyle.Image
            };
            var saveFileButton = new ToolStripButton(MainFormStrings.SaveFile, Resources.SaveFileIcon, (s, e) => SaveFile())
            {
                DisplayStyle = ToolStripItemDisplayStyle.Image
            };
            var exportButton = new ToolStripButton(MainFormStrings.Export, Resources.ExportIcon, (s, e) =>
            {
                if (LastExportData == null)
                {
                    ExportFile();
                    return;
                }

                CommitChanges();
                try
                {
                    LastExportData.Exporter.Export(LastExportData.OutputPath, ScoreBook);
                    MessageBox.Show(this, ErrorStrings.ReExportComplete, Program.ApplicationName, MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show(this, ErrorStrings.ExportFailed, Program.ApplicationName, MessageBoxButtons.OK, MessageBoxIcon.Error);
                    Program.DumpException(ex);
                }
            })
            {
                DisplayStyle = ToolStripItemDisplayStyle.Image
            };

            var cutButton = new ToolStripButton(MainFormStrings.Cut, Resources.CutIcon, (s, e) => noteView.CutSelectedNotes())
            {
                DisplayStyle = ToolStripItemDisplayStyle.Image
            };
            var copyButton = new ToolStripButton(MainFormStrings.Copy, Resources.CopyIcon, (s, e) => noteView.CopySelectedNotes())
            {
                DisplayStyle = ToolStripItemDisplayStyle.Image
            };
            var pasteButton = new ToolStripButton(MainFormStrings.Paste, Resources.PasteIcon, (s, e) => noteView.PasteNotes())
            {
                DisplayStyle = ToolStripItemDisplayStyle.Image
            };

            var undoButton = new ToolStripButton(MainFormStrings.Undo, Resources.UndoIcon, (s, e) => noteView.Undo())
            {
                DisplayStyle = ToolStripItemDisplayStyle.Image,
                Enabled = false
            };
            var redoButton = new ToolStripButton(MainFormStrings.Redo, Resources.RedoIcon, (s, e) => noteView.Redo())
            {
                DisplayStyle = ToolStripItemDisplayStyle.Image,
                Enabled = false
            };

            var penButton = new ToolStripButton(MainFormStrings.Pen, Resources.EditIcon, (s, e) => noteView.EditMode = EditMode.Edit)
            {
                DisplayStyle = ToolStripItemDisplayStyle.Image
            };
            var selectionButton = new ToolStripButton(MainFormStrings.Selection, Resources.SelectionIcon, (s, e) => noteView.EditMode = EditMode.Select)
            {
                DisplayStyle = ToolStripItemDisplayStyle.Image
            };
            var eraserButton = new ToolStripButton(MainFormStrings.Eraser, Resources.EraserIcon, (s, e) => noteView.EditMode = EditMode.Erase)
            {
                DisplayStyle = ToolStripItemDisplayStyle.Image
            };

            var zoomInButton = new ToolStripButton(MainFormStrings.ZoomIn, Resources.ZoomInIcon)
            {
                Enabled = noteView.UnitBeatHeight < 960,
                DisplayStyle = ToolStripItemDisplayStyle.Image
            };
            var zoomOutButton = new ToolStripButton(MainFormStrings.ZoomOut, Resources.ZoomOutIcon)
            {
                Enabled = noteView.UnitBeatHeight > 30,
                DisplayStyle = ToolStripItemDisplayStyle.Image
            };

            zoomInButton.Click += (s, e) =>
            {
                noteView.UnitBeatHeight *= 2;
                ApplicationSettings.Default.UnitBeatHeight = (int)noteView.UnitBeatHeight;
                zoomOutButton.Enabled = CanZoomOut;
                zoomInButton.Enabled = CanZoomIn;
                UpdateThumbHeight();
            };

            zoomOutButton.Click += (s, e) =>
            {
                noteView.UnitBeatHeight /= 2;
                ApplicationSettings.Default.UnitBeatHeight = (int)noteView.UnitBeatHeight;
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
                cutButton, copyButton, pasteButton, new ToolStripSeparator(),
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
            var slideStepButton = new ToolStripButton(MainFormStrings.SlideStep, Resources.SlideStepIcon, (s, e) =>
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
                new ToolStripMenuItem(MainFormStrings.AirUp, Resources.AirUpIcon, (s, e) => noteView.AirDirection = new AirDirection(VerticalAirDirection.Up, HorizontalAirDirection.Center)),
                new ToolStripMenuItem(MainFormStrings.AirLeftUp, Resources.AirLeftUpIcon, (s, e) => noteView.AirDirection = new AirDirection(VerticalAirDirection.Up, HorizontalAirDirection.Left)),
                new ToolStripMenuItem(MainFormStrings.AirRightUp, Resources.AirRightUpIcon, (s, e) => noteView.AirDirection = new AirDirection(VerticalAirDirection.Up, HorizontalAirDirection.Right)),
                new ToolStripMenuItem(MainFormStrings.AirDown, Resources.AirDownIcon, (s, e) => noteView.AirDirection = new AirDirection(VerticalAirDirection.Down, HorizontalAirDirection.Center)),
                new ToolStripMenuItem(MainFormStrings.AirLeftDown, Resources.AirLeftDownIcon, (s, e) => noteView.AirDirection = new AirDirection(VerticalAirDirection.Down, HorizontalAirDirection.Left)),
                new ToolStripMenuItem(MainFormStrings.AirRightDown, Resources.AirRightDownIcon, (s, e) => noteView.AirDirection = new AirDirection(VerticalAirDirection.Down, HorizontalAirDirection.Right))
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
            quantizeComboBox.Items.AddRange(quantizeTicks.Select(p => p + MainFormStrings.Division).ToArray());
            quantizeComboBox.Items.Add(MainFormStrings.Custom);
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
                noteView.Focus();
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

    internal class ExportData
    {
        public string OutputPath { get; set; }
        public Components.Exporter.IExporter Exporter { get; set; }
    }
}
