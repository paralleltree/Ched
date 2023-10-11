using System;
using System.Collections.Generic;
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
using Ched.UI.Shortcuts;
using Ched.UI.Operations;
using Ched.UI.Windows;
using System.Globalization;
using Ched.Drawing;
using System.Runtime.CompilerServices;
using System.Configuration;

namespace Ched.UI
{
    public partial class MainForm : Form
    {
        private event EventHandler PreviewModeChanged;

        private readonly string UserShortcutKeySourcePath = "keybindings.json";
        private readonly string FileExtension = ".chs";
        private string FileTypeFilter => FileFilterStrings.ChedFilter + string.Format("({0})|{1}", "*" + FileExtension, "*" + FileExtension);

        private bool isPreviewMode;

        private ScoreBook ScoreBook { get; set; }
        private OperationManager OperationManager { get; }

        private ScrollBar NoteViewScrollBar { get; }
        private NoteView NoteView { get; }

        private SoundPreviewManager PreviewManager { get; }
        private SoundSource CurrentMusicSource;

        private ShortcutManagerHost ShortcutManagerHost { get; }
        private ShortcutManager ShortcutManager => ShortcutManagerHost.ShortcutManager;

        private ExportManager ExportManager { get; } = new ExportManager();

        private int Channel { get; set; } = 1;
        private int ViewChannel { get; set; } = 0;

        private float WidthAmount { get; set; } = 1;

        private bool LaneVisual { get; set; } = false;
         
        private int defaultCh = 1;


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
                NoteView.UnitLaneWidth = isPreviewMode ? 4 : ApplicationSettings.Default.UnitLaneWidth;
                NoteView.ShortNoteHeight = isPreviewMode ? 4 : 5;
                NoteView.UnitBeatHeight = isPreviewMode ? 48 : ApplicationSettings.Default.UnitBeatHeight;
                UpdateThumbHeight();
                PreviewModeChanged?.Invoke(this, EventArgs.Empty);
            }
        }

        private bool CanWidenLaneWidth => !IsPreviewMode && NoteView.UnitLaneWidth < 24;
        private bool CanNarrowLaneWidth => !IsPreviewMode && NoteView.UnitLaneWidth > 12;
        private bool CanZoomIn => !IsPreviewMode && NoteView.UnitBeatHeight < 9600;
        private bool CanZoomOut => !IsPreviewMode && NoteView.UnitBeatHeight > 30;
        private bool CanEdit => !IsPreviewMode && !PreviewManager.Playing;

        public MainForm()
        {
            InitializeComponent();
            Size = new Size(420, 700);
            Icon = Resources.MainIcon;

            ToolStripManager.RenderMode = ToolStripManagerRenderMode.System;

            OperationManager = new OperationManager();
            OperationManager.OperationHistoryChanged += (s, e) =>
            {
                SetText(ScoreBook.Path);
                NoteView.Invalidate();
            };
            OperationManager.ChangesCommitted += (s, e) => SetText(ScoreBook.Path);

            NoteView = new NoteView(OperationManager)
            {
                Dock = DockStyle.Fill,
                UnitBeatHeight = ApplicationSettings.Default.UnitBeatHeight,
                UnitLaneWidth = ApplicationSettings.Default.UnitLaneWidth,
                InsertAirWithAirAction = ApplicationSettings.Default.InsertAirWithAirAction
            };

            PreviewManager = new SoundPreviewManager(this);
            PreviewManager.IsStopAtLastNote = ApplicationSettings.Default.IsPreviewAbortAtLastNote;
            PreviewManager.TickUpdated += (s, e) => NoteView.CurrentTick = e.Tick;
            PreviewManager.ExceptionThrown += (s, e) => MessageBox.Show(this, ErrorStrings.PreviewException, Program.ApplicationName, MessageBoxButtons.OK, MessageBoxIcon.Error);

            var commandSource = new ShortcutCommandSource();
            SetupCommands(commandSource);
            var shortcutManager = new ShortcutManager()
            {
                DefaultKeySource = new DefaultShortcutKeySource(),
                CommandSource = commandSource
            };
            ShortcutManagerHost = new ShortcutManagerHost(shortcutManager);
            ShortcutManagerHost.UserShortcutKeySource = LoadUserShortcutKeySource();

            NoteViewScrollBar = new VScrollBar()
            {
                Dock = DockStyle.Right,
                Minimum = -NoteView.UnitBeatTick * 4 * 20,
                SmallChange = NoteView.UnitBeatTick
            };

            void processScrollBarRangeExtension(ScrollBar s)
            {
                if (NoteViewScrollBar.Value < NoteViewScrollBar.Minimum * 0.9f)
                {
                    NoteViewScrollBar.Minimum = (int)(NoteViewScrollBar.Minimum * 1.2);
                }
            }

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

            NoteView.NewNoteTypeChanged += (s, e) => NoteView.EditMode = EditMode.Edit;

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
                if (!ConfirmDiscardChanges()) return;
                LoadFile(path);
            };

            FormClosing += (s, e) =>
            {
                if (!ConfirmDiscardChanges())
                {
                    e.Cancel = true;
                    return;
                }

                ApplicationSettings.Default.Save();
                File.WriteAllText(UserShortcutKeySourcePath, ShortcutManagerHost.UserShortcutKeySource.DumpShortcutKeys());
            };

            using (var manager = this.WorkWithLayout())
            {
                this.MainMenuStrip = CreateMainMenu(NoteView);
                this.Controls.Add(NoteView);
                this.Controls.Add(NoteViewScrollBar);
                this.Controls.Add(CreateNewNoteTypeToolStrip(NoteView));
                this.Controls.Add(CreateMainToolStrip(NoteView));
                this.Controls.Add(MainMenuStrip);
            }

            NoteView.NewNoteType = NoteType.Tap;
            NoteView.EditMode = EditMode.Edit;

            LoadEmptyBook();
            ShortcutManager.NotifyUpdateShortcut();
            SetText();

            if (!PreviewManager.IsSupported)
                MessageBox.Show(this, ErrorStrings.PreviewNotSupported, Program.ApplicationName, MessageBoxButtons.OK, MessageBoxIcon.Information);

            if (PluginManager.FailedFiles.Count > 0)
            {
                MessageBox.Show(this, string.Join("\n", new[] { ErrorStrings.PluginLoadError }.Concat(PluginManager.FailedFiles)), Program.ApplicationName, MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
            if (PluginManager.InvalidFiles.Count > 0)
            {
                MessageBox.Show(this, string.Join("\n", new[] { ErrorStrings.PluginNotSupported }.Concat(PluginManager.InvalidFiles)), Program.ApplicationName, MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        public MainForm(string filePath) : this()
        {
            LoadFile(filePath);
        }

        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            if (ShortcutManager.ExecuteCommand(keyData)) return true;
            return base.ProcessCmdKey(ref msg, keyData);
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
                if (ScoreBook.IsUpgradeNeeded(filePath))
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
            ExportManager.Load(book);
            NoteView.Initialize(book.Score);
            NoteViewScrollBar.Value = NoteViewScrollBar.GetMaximumValue();
            NoteViewScrollBar.Minimum = -Math.Max(NoteView.UnitBeatTick * 4 * 20, NoteView.Notes.GetLastTick());
            NoteViewScrollBar.SmallChange = NoteView.UnitBeatTick;
            UpdateThumbHeight();
            SetText(book.Path);
            CurrentMusicSource = new SoundSource();
            if (!string.IsNullOrEmpty(book.Path))
            {
                SoundSettings.Default.ScoreSound.TryGetValue(book.Path, out SoundSource src);
                if (src != null)
                {
                    if (src.Volume == 0) src.Volume = 1;
                    CurrentMusicSource = src;
                }
            }
        }

        protected void LoadEmptyBook()
        {
            var book = new ScoreBook();
            var events = book.Score.Events;
            events.BpmChangeEvents.Add(new BpmChangeEvent() { Tick = 0, Bpm = 120 });
            events.TimeSignatureChangeEvents.Add(new TimeSignatureChangeEvent() { Tick = 0, Numerator = 4, DenominatorExponent = 2 });
            LoadBook(book);
        }

        protected void OpenFile()
        {
            if (!ConfirmDiscardChanges()) return;
            if (!TrySelectOpeningFile(FileTypeFilter, out string path)) return;
            LoadFile(path);
        }

        protected bool TrySelectOpeningFile(string filter, out string path)
        {
            path = null;

            var dialog = new OpenFileDialog()
            {
                Filter = filter
            };

            if (dialog.ShowDialog(this) == DialogResult.OK)
            {
                path = dialog.FileName;
                return true;
            }
            return false;
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
            OperationManager.CommitChanges();

            SoundSettings.Default.ScoreSound[ScoreBook.Path] = CurrentMusicSource;
            SoundSettings.Default.Save();
        }

        protected void ExportAs(IScoreBookExportPlugin exportPlugin)
        {
            var dialog = new SaveFileDialog() { Filter = exportPlugin.FileFilter };
            if (dialog.ShowDialog(this) != DialogResult.OK) return;

            HandleExport(ScoreBook, ExportManager.PrepareExport(exportPlugin, dialog.FileName));
        }

        private void HandleExport(ScoreBook book, ExportContext context)
        {
            CommitChanges();
            string message;
            bool hasError = true;
            try
            {
                context.Export(book);
                message = ErrorStrings.ExportComplete;
                hasError = false;
                ExportManager.CommitExported(context);
            }
            catch (UserCancelledException)
            {
                // Do nothing
                return;
            }
            catch (InvalidTimeSignatureException ex)
            {
                int beatAt = ex.Tick / ScoreBook.Score.TicksPerBeat + 1;
                message = string.Format(ErrorStrings.InvalidTimeSignature, beatAt);
            }
            catch (Exception ex)
            {
                Program.DumpExceptionTo(ex, "export_exception.json");
                message = ErrorStrings.ExportFailed + Environment.NewLine + ex.Message;
            }

            ShowDiagnosticsResult(MainFormStrings.Export, message, hasError, context.Diagnostics);
        }

        protected void HandleImport(IScoreBookImportPlugin plugin, ScoreBookImportPluginArgs args)
        {
            string message;
            bool hasError = true;
            try
            {
                var book = plugin.Import(args);
                LoadBook(book);
                message = ErrorStrings.ImportComplete;
                hasError = false;
            }
            catch (Exception ex)
            {
                Program.DumpExceptionTo(ex, "import_exception.json");
                LoadEmptyBook();
                message = ErrorStrings.ImportFailed + Environment.NewLine + ex.Message;
            }

            ShowDiagnosticsResult(MainFormStrings.Import, message, hasError, args.Diagnostics);
        }

        protected void ShowDiagnosticsResult(string title, string message, bool hasError, IReadOnlyCollection<Diagnostic> diagnostics)
        {
            if (diagnostics.Count > 0)
            {
                var vm = new DiagnosticsWindowViewModel()
                {
                    Title = title,
                    Message = message,
                    Diagnostics = new System.Collections.ObjectModel.ObservableCollection<Diagnostic>(diagnostics)
                };
                var window = new DiagnosticsWindow()
                {
                    DataContext = vm
                };
                window.ShowDialog(this);
            }
            else
            {
                MessageBox.Show(this, message, title, MessageBoxButtons.OK, hasError ? MessageBoxIcon.Error : MessageBoxIcon.Information);
            }
        }

        protected void CommitChanges()
        {
            ScoreBook.Score.Notes = NoteView.Notes.Reposit();
            // Eventsは参照渡ししてますよん
        }

        protected void ClearFile()
        {
            if (!ConfirmDiscardChanges()) return;
            LoadEmptyBook();
        }

        protected bool ConfirmDiscardChanges()
        {
            if (!OperationManager.IsChanged) return true;
            return MessageBox.Show(this, ErrorStrings.FileDiscardConfirmation, Program.ApplicationName, MessageBoxButtons.OKCancel, MessageBoxIcon.Warning, MessageBoxDefaultButton.Button2) == DialogResult.OK;
        }

        protected void SetText()
        {
            SetText(null);
        }

        protected void SetText(string filePath)
        {
            Text = "Ched SkEdition" + (string.IsNullOrEmpty(filePath) ? "" : " - " + Path.GetFileName(filePath)) + (OperationManager.IsChanged ? " *" : "");
        }

        private void UpdateThumbHeight()
        {
            NoteViewScrollBar.LargeChange = NoteView.TailTick - NoteView.HeadTick;
            NoteViewScrollBar.Maximum = NoteViewScrollBar.LargeChange + NoteView.PaddingHeadTick;
        }

        private void PlayPreview()
        {
            if (string.IsNullOrEmpty(CurrentMusicSource?.FilePath))
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

            int startTick = NoteView.CurrentTick;
            void lambda(object p, EventArgs q)
            {
                PreviewManager.Finished -= lambda;
                NoteView.CurrentTick = startTick;
                NoteView.Editable = CanEdit;
            }

            try
            {
                CommitChanges();
                var context = new SoundPreviewContext(ScoreBook.Score, CurrentMusicSource, SoundSettings.Default.GuideSound);
                
                if (!PreviewManager.Start(context, startTick)) return;
                PreviewManager.Finished += lambda;
                NoteView.Editable = CanEdit;
            }
            catch (Exception ex)
            {
                Program.DumpExceptionTo(ex, "sound_exception.json");
            }
        }

        private UserShortcutKeySource LoadUserShortcutKeySource()
        {
            if (File.Exists(UserShortcutKeySourcePath))
            {
                return new UserShortcutKeySource(File.ReadAllText(UserShortcutKeySourcePath));
            }
            return new UserShortcutKeySource();
        }

        private void SetupCommands(ShortcutCommandSource commandSource)
        {
            commandSource.RegisterCommand(Commands.NewFile, MainFormStrings.NewFile, ClearFile);
            commandSource.RegisterCommand(Commands.OpenFile, MainFormStrings.OpenFile, OpenFile);
            commandSource.RegisterCommand(Commands.Save, MainFormStrings.SaveFile, SaveFile);
            commandSource.RegisterCommand(Commands.SaveAs, MainFormStrings.SaveAs, SaveAs);
            commandSource.RegisterCommand(Commands.ReExport, MainFormStrings.Export, () =>
            {
                if (!ExportManager.CanReExport)
                {
                    if (PluginManager.ScoreBookExportPlugins.Count() == 1)
                    {
                        ExportAs(PluginManager.ScoreBookExportPlugins.Single());
                        return;
                    }
                    MessageBox.Show(this, ErrorStrings.NotExported, Program.ApplicationName, MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                HandleExport(ScoreBook, ExportManager.PrepareReExport());
            });
            commandSource.RegisterCommand(Commands.ShowScoreBookProperties, MainFormStrings.BookProperty, () =>
            {
                var vm = new BookPropertiesWindowViewModel(ScoreBook, CurrentMusicSource);
                var window = new BookPropertiesWindow() { DataContext = vm };
                window.ShowDialog(this);
            });
            commandSource.RegisterCommand(Commands.ShowShortcutSettings, MainFormStrings.KeyboardShortcuts, () => ConfigureKeyboardShortcut());

            commandSource.RegisterCommand(Commands.Undo, MainFormStrings.Undo, () => { if (OperationManager.CanUndo) OperationManager.Undo(); });
            commandSource.RegisterCommand(Commands.Redo, MainFormStrings.Redo, () => { if (OperationManager.CanRedo) OperationManager.Redo(); });

            commandSource.RegisterCommand(Commands.Cut, MainFormStrings.Cut, () => NoteView.CutSelectedNotes());
            commandSource.RegisterCommand(Commands.Copy, MainFormStrings.Copy, () => NoteView.CopySelectedNotes());
            commandSource.RegisterCommand(Commands.Paste, MainFormStrings.Paste, () => NoteView.PasteNotes());
            commandSource.RegisterCommand(Commands.PasteFlip, MainFormStrings.PasteFlipped, () => NoteView.PasteFlippedNotes());

            commandSource.RegisterCommand(Commands.SelectAll, MainFormStrings.SelectAll, () => NoteView.SelectAll());
            commandSource.RegisterCommand(Commands.SelectToBegin, MainFormStrings.SelectToBeginning, () => NoteView.SelectToBeginning());
            commandSource.RegisterCommand(Commands.SelectToEnd, MainFormStrings.SelectToEnd, () => NoteView.SelectToEnd());

            commandSource.RegisterCommand(Commands.FlipSelectedNotes, MainFormStrings.FlipSelectedNotes, () => NoteView.FlipSelectedNotes());
            commandSource.RegisterCommand(Commands.RemoveSelectedNotes, MainFormStrings.RemoveSelectedNotes, () => NoteView.RemoveSelectedNotes());
            commandSource.RegisterCommand(Commands.RemoveSelectedEvents, MainFormStrings.RemoveEvents, () => NoteView.RemoveSelectedEvents());
            commandSource.RegisterCommand(Commands.ChangeChannelSelectedNotes, MainFormStrings.ChangeChannelSelectedNotes, () => NoteView.ChangeChannelSelectedNotes());

            commandSource.RegisterCommand(Commands.SwitchScorePreviewMode, MainFormStrings.ScorePreview, () => IsPreviewMode = !IsPreviewMode);

            commandSource.RegisterCommand(Commands.WidenLaneWidth, MainFormStrings.WidenLaneWidth, () =>
            {
                if (!CanWidenLaneWidth) return;
                NoteView.UnitLaneWidth += 4;
                ApplicationSettings.Default.UnitLaneWidth = NoteView.UnitLaneWidth;
            });
            commandSource.RegisterCommand(Commands.NarrowLaneWidth, MainFormStrings.NarrowLaneWidth, () =>
            {
                if (!CanNarrowLaneWidth) return;
                NoteView.UnitLaneWidth -= 4;
                ApplicationSettings.Default.UnitLaneWidth = NoteView.UnitLaneWidth;
            });

            commandSource.RegisterCommand(Commands.InsertBpmChange, "BPM", () =>
            {
                var form = new BpmSelectionForm()
                {
                    Bpm = NoteView.ScoreEvents.BpmChangeEvents.OrderBy(p => p.Tick).LastOrDefault(p => p.Tick <= NoteView.CurrentTick)?.Bpm ?? 120
                };
                if (form.ShowDialog(this) != DialogResult.OK) return;

                var item = new BpmChangeEvent()
                {
                    Tick = NoteView.CurrentTick,
                    Bpm = form.Bpm
                };
                UpdateEvent(NoteView.ScoreEvents.BpmChangeEvents, item);
            });
            commandSource.RegisterCommand(Commands.InsertHighSpeedChange, MainFormStrings.HighSpeed, () =>
            {
                var form = new HighSpeedSelectionForm()
                {
                    SpeedRatio = NoteView.ScoreEvents.HighSpeedChangeEvents.OrderBy(p => p.Tick).LastOrDefault(p => p.Tick <= NoteView.CurrentTick)?.SpeedRatio ?? 1.0m,
                    SpeedCh = Channel
                };
                if (form.ShowDialog(this) != DialogResult.OK) return;

                var item = new HighSpeedChangeEvent()
                {
                    Tick = NoteView.CurrentTick,
                    SpeedRatio = form.SpeedRatio,
                    SpeedCh = form.SpeedCh,
                    Type = form.SpeedCh
                };
                UpdateEvent(NoteView.ScoreEvents.HighSpeedChangeEvents, item);
            });
            commandSource.RegisterCommand(Commands.InsertTimeSignatureChange, MainFormStrings.TimeSignature, () =>
            {
                var form = new TimeSignatureSelectionForm();
                if (form.ShowDialog(this) != DialogResult.OK) return;

                var item = new TimeSignatureChangeEvent()
                {
                    Tick = NoteView.CurrentTick,
                    Numerator = form.Numerator,
                    DenominatorExponent = form.DenominatorExponent                  
                };
                UpdateEvent(NoteView.ScoreEvents.TimeSignatureChangeEvents, item);
            });

            void UpdateEvent<T>(List<T> list, T item) where T : EventBase
            {

                var prev = list.SingleOrDefault(p => p.Tick == item.Tick && p.Type == item.Type);

                var insertOp = new InsertEventOperation<T>(list, item);
                if (prev == null)
                {
                    OperationManager.InvokeAndPush(insertOp);
                }
                else
                {
                    var removeOp = new RemoveEventOperation<T>(list, prev);
                    OperationManager.InvokeAndPush(new CompositeOperation(insertOp.Description, new IOperation[] { removeOp, insertOp }));
                }
                NoteView.Invalidate();
            }

            commandSource.RegisterCommand(Commands.PlayPreview, MainFormStrings.Play, () => PlayPreview());

            commandSource.RegisterCommand(Commands.ShowHelp, MainFormStrings.Help, () => System.Diagnostics.Process.Start("https://github.com/myuuskye/Ched/wiki"));

            commandSource.RegisterCommand(Commands.SelectPen, MainFormStrings.Pen, () => NoteView.EditMode = EditMode.Edit);
            commandSource.RegisterCommand(Commands.SelectSelection, MainFormStrings.Selection, () => NoteView.EditMode = EditMode.Select);
            commandSource.RegisterCommand(Commands.SelectEraser, MainFormStrings.Eraser, () => NoteView.EditMode = EditMode.Erase);

            commandSource.RegisterCommand(Commands.ZoomIn, MainFormStrings.ZoomIn, () =>
            {
                if (!CanZoomIn) return;
                NoteView.UnitBeatHeight *= 2;
                ApplicationSettings.Default.UnitBeatHeight = (int)NoteView.UnitBeatHeight;
                UpdateThumbHeight();
            });
            commandSource.RegisterCommand(Commands.ZoomOut, MainFormStrings.ZoomOut, () =>
            {
                if (!CanZoomOut) return;
                NoteView.UnitBeatHeight /= 2;
                ApplicationSettings.Default.UnitBeatHeight = (int)NoteView.UnitBeatHeight;
                UpdateThumbHeight();
            });

            commandSource.RegisterCommand(Commands.SelectTap, "TAP", () =>
            {
                NoteView.NewNoteType = NoteType.Tap;
                NoteView.IsNewNoteStart = false;
            });
            commandSource.RegisterCommand(Commands.SelectExTap, "ExTAP", () =>
            {
                NoteView.NewNoteType = NoteType.ExTap;
                NoteView.IsNewNoteStart = false;
            });
            commandSource.RegisterCommand(Commands.SelectHold, "HOLD", () => NoteView.NewNoteType = NoteType.Hold);
            commandSource.RegisterCommand(Commands.SelectSlide, "SLIDE", () =>
            {
                NoteView.NewNoteType = NoteType.Slide;
                NoteView.IsNewSlideStepVisible = false;
            });
            commandSource.RegisterCommand(Commands.SelectSlideStep, MainFormStrings.SlideStep, () =>
            {
                NoteView.NewNoteType = NoteType.Slide;
                NoteView.IsNewSlideStepVisible = true;
            });
            commandSource.RegisterCommand(Commands.SelectAir, "AIR", () =>
            {
                if (NoteView.NewNoteType != NoteType.Air)
                {
                    NoteView.NewNoteType = NoteType.Air;
                    return;
                }
                if (NoteView.AirDirection.HorizontalDirection == HorizontalAirDirection.Left)
                {
                    NoteView.AirDirection = new AirDirection(
                        NoteView.AirDirection.VerticalDirection == VerticalAirDirection.Up ? VerticalAirDirection.Down : VerticalAirDirection.Up,
                        GetNextHorizontalDirection(NoteView.AirDirection.HorizontalDirection));
                    return;
                }
                HandleHorizontalAirDirection(NoteView.AirDirection.VerticalDirection);
            });
            commandSource.RegisterCommand(Commands.SelectAirUp, MainFormStrings.AirUp, () => HandleHorizontalAirDirection(VerticalAirDirection.Up));
            commandSource.RegisterCommand(Commands.SelectAirDown, MainFormStrings.AirDown, () => HandleHorizontalAirDirection(VerticalAirDirection.Down));
            commandSource.RegisterCommand(Commands.SelectAirAction, "AIR-ACTION", () => NoteView.NewNoteType = NoteType.AirAction);
            commandSource.RegisterCommand(Commands.SelectFlick, "FLICK", () => NoteView.NewNoteType = NoteType.Flick);
            commandSource.RegisterCommand(Commands.SelectDamage, "DAMAGE", () => NoteView.NewNoteType = NoteType.Damage);

            commandSource.RegisterCommand(Commands.SelectGuide, "GUIDE", () =>
            {
                NoteView.NewNoteType = NoteType.Guide;
                NoteView.IsNewGuideStepVisible = false;
            });
            commandSource.RegisterCommand(Commands.SelectGuideStep, "GUIDESTEP", () =>
            {
                NoteView.NewNoteType = NoteType.Guide;
                NoteView.IsNewGuideStepVisible = true;
            });

            commandSource.RegisterCommand(Commands.SelectTap2, "TAP2", () => 
            {
                NoteView.NewNoteType = NoteType.Tap;
                NoteView.IsNewNoteStart = true;
            });
            commandSource.RegisterCommand(Commands.SelectExTap2, "ExTAP2", () =>
            {
                NoteView.NewNoteType = NoteType.ExTap;
                NoteView.IsNewNoteStart = true;
            });


            void HandleHorizontalAirDirection(VerticalAirDirection verticalDirection)
            {
                if (NoteView.NewNoteType != NoteType.Air)
                {
                    NoteView.NewNoteType = NoteType.Air;
                    NoteView.AirDirection = new AirDirection(verticalDirection, NoteView.AirDirection.HorizontalDirection);
                    return;
                }
                var horizontalDirection = NoteView.AirDirection.HorizontalDirection;
                if (verticalDirection == NoteView.AirDirection.VerticalDirection)
                    horizontalDirection = GetNextHorizontalDirection(horizontalDirection);
                NoteView.AirDirection = new AirDirection(verticalDirection, horizontalDirection);
            }

            HorizontalAirDirection GetNextHorizontalDirection(HorizontalAirDirection direction)
            {
                switch (direction)
                {
                    case HorizontalAirDirection.Center:
                        return HorizontalAirDirection.Right;

                    case HorizontalAirDirection.Right:
                        return HorizontalAirDirection.Left;

                    case HorizontalAirDirection.Left:
                        return HorizontalAirDirection.Center;
                }
                throw new ArgumentException();
            }
        }

        private void ConfigureKeyboardShortcut()
        {
            var vm = new ShortcutSettingsWindowViewModel(ShortcutManagerHost);
            var window = new ShortcutSettingsWindow() { DataContext = vm };
            window.ShowDialog(this);
            ShortcutManager.NotifyUpdateShortcut();
        }

        private MenuStrip CreateMainMenu(NoteView noteView)
        {
            var shortcutItemBuilder = new ToolStripMenuItemBuilder(ShortcutManager);

            var importPluginItems = PluginManager.ScoreBookImportPlugins.Select(p => new ToolStripMenuItem(p.DisplayName, null, (s, e) =>
            {
                if (!ConfirmDiscardChanges()) return;
                if (!TrySelectOpeningFile(p.FileFilter, out string path)) return;

                using (var stream = new FileStream(path, FileMode.Open, FileAccess.Read))
                {
                    var args = new ScoreBookImportPluginArgs(stream);
                    HandleImport(p, args);
                }
            })).ToArray();

            var exportPluginItems = PluginManager.ScoreBookExportPlugins.Select(p => new ToolStripMenuItem(p.DisplayName, null, (s, e) =>
            {
                ExportAs(p);
            })).ToArray();

            var bookPropertiesMenuItem = shortcutItemBuilder.BuildItem(Commands.ShowScoreBookProperties, MainFormStrings.BookProperty);

            var fileMenuItems = new ToolStripItem[]
            {
                shortcutItemBuilder.BuildItem(Commands.NewFile, MainFormStrings.NewFile + "(&N)"),
                shortcutItemBuilder.BuildItem(Commands.OpenFile, MainFormStrings.OpenFile + "(&O)"),
                shortcutItemBuilder.BuildItem(Commands.Save, MainFormStrings.SaveFile + "(&S)"),
                shortcutItemBuilder.BuildItem(Commands.SaveAs, MainFormStrings.SaveAs + "(&A)"),
                new ToolStripSeparator(),
                new ToolStripMenuItem(MainFormStrings.Import, null, importPluginItems) { Enabled = importPluginItems.Length > 0 },
                new ToolStripMenuItem(MainFormStrings.Export, null, exportPluginItems) { Enabled = exportPluginItems.Length > 0 },
                new ToolStripSeparator(),
                bookPropertiesMenuItem,
                new ToolStripSeparator(),
                shortcutItemBuilder.BuildItem(Commands.ShowShortcutSettings, MainFormStrings.KeyboardShortcuts),
                new ToolStripSeparator(),
                new ToolStripMenuItem(MainFormStrings.Exit + "(&X)", null, (s, e) => this.Close())
            };

            var undoItem = shortcutItemBuilder.BuildItem(Commands.Undo, MainFormStrings.Undo);
            undoItem.Enabled = false;

            var redoItem = shortcutItemBuilder.BuildItem(Commands.Redo, MainFormStrings.Redo);
            redoItem.Enabled = false;

            var cutItem = shortcutItemBuilder.BuildItem(Commands.Cut, MainFormStrings.Cut);
            var copyItem = shortcutItemBuilder.BuildItem(Commands.Copy, MainFormStrings.Copy);
            var pasteItem = shortcutItemBuilder.BuildItem(Commands.Paste, MainFormStrings.Paste);
            var pasteFlippedItem = shortcutItemBuilder.BuildItem(Commands.PasteFlip, MainFormStrings.PasteFlipped);

            var selectAllItem = shortcutItemBuilder.BuildItem(Commands.SelectAll, MainFormStrings.SelectAll);
            var selectToEndItem = shortcutItemBuilder.BuildItem(Commands.SelectToEnd, MainFormStrings.SelectToEnd);
            var selectoToBeginningItem = shortcutItemBuilder.BuildItem(Commands.SelectToBegin, MainFormStrings.SelectToBeginning);

            var flipSelectedNotesItem = shortcutItemBuilder.BuildItem(Commands.FlipSelectedNotes, MainFormStrings.FlipSelectedNotes);
            var removeSelectedNotesItem = shortcutItemBuilder.BuildItem(Commands.RemoveSelectedNotes, MainFormStrings.RemoveSelectedNotes);
            var removeEventsItem = shortcutItemBuilder.BuildItem(Commands.RemoveSelectedEvents, MainFormStrings.RemoveEvents);
            var changeChannelSelectedNotesItem = shortcutItemBuilder.BuildItem(Commands.ChangeChannelSelectedNotes, MainFormStrings.ChangeChannelSelectedNotes);

            var insertAirWithAirActionItem = new ToolStripMenuItem(MainFormStrings.InsertAirWithAirAction, null, (s, e) =>
            {
                var item = s as ToolStripMenuItem;
                item.Checked = !item.Checked;
                NoteView.InsertAirWithAirAction = item.Checked;
                ApplicationSettings.Default.InsertAirWithAirAction = item.Checked;
            })
            {
                Checked = ApplicationSettings.Default.InsertAirWithAirAction
            };

            var pluginItems = PluginManager.ScorePlugins.Select(p => new ToolStripMenuItem(p.DisplayName, null, (s, e) =>
            {
                CommitChanges();
                void updateScore(Score newScore)
                {
                    var op = new UpdateScoreOperation(ScoreBook.Score, newScore, score =>
                    {
                        ScoreBook.Score = score;
                        noteView.UpdateScore(score);
                    });
                    OperationManager.InvokeAndPush(op);
                }

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
            var pluginItem = new ToolStripMenuItem(MainFormStrings.Plugin, null, pluginItems) { Enabled = pluginItems.Length > 0 };

            var editMenuItems = new ToolStripItem[]
            {
                undoItem, redoItem, new ToolStripSeparator(),
                cutItem, copyItem, pasteItem, pasteFlippedItem, new ToolStripSeparator(),
                selectAllItem, selectToEndItem, selectoToBeginningItem, new ToolStripSeparator(),
                flipSelectedNotesItem, removeSelectedNotesItem, removeEventsItem, new ToolStripSeparator(),
                insertAirWithAirActionItem, new ToolStripSeparator(),
                pluginItem
            };

            var viewModeItem = shortcutItemBuilder.BuildItem(Commands.SwitchScorePreviewMode, MainFormStrings.ScorePreview);
            PreviewModeChanged += (s, e) => viewModeItem.Checked = IsPreviewMode;

            var widenLaneWidthMenuItem = shortcutItemBuilder.BuildItem(Commands.WidenLaneWidth, MainFormStrings.WidenLaneWidth);
            var narrowLaneWidthMenuItem = shortcutItemBuilder.BuildItem(Commands.NarrowLaneWidth, MainFormStrings.NarrowLaneWidth);

            NoteView.UnitLaneWidthChanged += (s, e) =>
            {
                widenLaneWidthMenuItem.Enabled = CanWidenLaneWidth;
                narrowLaneWidthMenuItem.Enabled = CanNarrowLaneWidth;
            };

            var viewMenuItems = new ToolStripItem[]
            {
                viewModeItem,
                new ToolStripSeparator(),
                widenLaneWidthMenuItem, narrowLaneWidthMenuItem
            };


            var insertBpmItem = shortcutItemBuilder.BuildItem(Commands.InsertBpmChange, "BPM");
            var insertHighSpeedItem = shortcutItemBuilder.BuildItem(Commands.InsertHighSpeedChange, MainFormStrings.HighSpeed);
            var insertTimeSignatureItem = shortcutItemBuilder.BuildItem(Commands.InsertTimeSignatureChange, MainFormStrings.TimeSignature);


            var insertMenuItems = new ToolStripItem[] { insertBpmItem, insertHighSpeedItem, insertTimeSignatureItem };
            

            var playItem = shortcutItemBuilder.BuildItem(Commands.PlayPreview, MainFormStrings.Play);

            var stopItem = new ToolStripMenuItem(MainFormStrings.Stop, null, (s, e) => PreviewManager.Stop());


            Rectangle ImageSize = new Rectangle(0, 0, 16, 16);

            Bitmap img = new Bitmap(16, 16);
            Graphics graph = Graphics.FromImage(img);
            graph.FillRectangle(Brushes.Black, ImageSize);

            Bitmap img2 = new Bitmap(16, 16);
            Graphics graph2 = Graphics.FromImage(img2);
            graph2.FillRectangle(Brushes.White, ImageSize);



            var themeBlack = new ToolStripMenuItem(MainFormStrings.ThemeBlack, img, (s, e) => noteView.Theme = 0);
            var themeWhite = new ToolStripMenuItem(MainFormStrings.ThemeWhite, img2, (s, e) => noteView.Theme = 1);
            var themePJsekai = new ToolStripMenuItem(MainFormStrings.PJsekai, img2, (s, e) => noteView.Theme = 2);

            var slowDownPreviewItem = new ToolStripMenuItem(MainFormStrings.SlowDownPreview, null, (s, e) =>
            {
                var item = s as ToolStripMenuItem;
                item.Checked = !item.Checked;
                ApplicationSettings.Default.IsSlowDownPreviewEnabled = item.Checked;
            })
            {
                Checked = ApplicationSettings.Default.IsSlowDownPreviewEnabled
            };

            var isAbortAtLastNoteItem = new ToolStripMenuItem(MainFormStrings.AbortAtLastNote, null, (s, e) =>
            {
                var item = s as ToolStripMenuItem;
                item.Checked = !item.Checked;
                PreviewManager.IsStopAtLastNote = item.Checked;
                ApplicationSettings.Default.IsPreviewAbortAtLastNote = item.Checked;
            })
            {
                Checked = ApplicationSettings.Default.IsPreviewAbortAtLastNote
            };

            PreviewManager.Started += (s, e) => isAbortAtLastNoteItem.Enabled = false;
            PreviewManager.Finished += (s, e) => isAbortAtLastNoteItem.Enabled = true;

            var channelMovableItem = new ToolStripMenuItem(MainFormStrings.ChannelMovable, null, (s, e) =>
            {
                var item = s as ToolStripMenuItem;
                item.Checked = !item.Checked;
                ApplicationSettings.Default.IsAnotherChannelEditable = item.Checked;
                noteView.EditbyCh = item.Checked;
            })
            {
                Checked = ApplicationSettings.Default.IsAnotherChannelEditable
            };

            noteView.NoteVisualMode = ApplicationSettings.Default.NoteVisualMode;
            var noteVisualModeItems = new ToolStripMenuItem[]
            {
                new ToolStripMenuItem(MainFormStrings.Visual1, null, (s, e) =>
                {
                var item = s as ToolStripMenuItem;
                ApplicationSettings.Default.NoteVisualMode = 0;
                noteView.NoteVisualMode = 0;
                }),
                new ToolStripMenuItem(MainFormStrings.Visual2, null, (s, e) =>
                {
                var item = s as ToolStripMenuItem;
                ApplicationSettings.Default.NoteVisualMode = 1;
                noteView.NoteVisualMode = 1;
                }),
                new ToolStripMenuItem(MainFormStrings.Visual3, null, (s, e) =>
                {
                var item = s as ToolStripMenuItem;
                ApplicationSettings.Default.NoteVisualMode = 2;
                noteView.NoteVisualMode = 2;
                }),
        };
            var noteVisualModeItem = new ToolStripMenuItem(MainFormStrings.ChannelNote, null, noteVisualModeItems);


            var playMenuItems = new ToolStripItem[]
            {
                playItem, stopItem, new ToolStripSeparator(),
                slowDownPreviewItem, isAbortAtLastNoteItem
            };

            var helpMenuItems = new ToolStripItem[]
            {
                shortcutItemBuilder.BuildItem(Commands.ShowHelp, MainFormStrings.Help),
                new ToolStripMenuItem(MainFormStrings.VersionInfo, null, (s, e) => new VersionInfoForm().ShowDialog(this))
            };

            var themeMenuItems = new ToolStripItem[] { themeBlack, themeWhite};

            var channelMenuItems = new ToolStripItem[] { channelMovableItem, noteVisualModeItem, changeChannelSelectedNotesItem };



            OperationManager.OperationHistoryChanged += (s, e) =>
            {
                redoItem.Enabled = OperationManager.CanRedo;
                undoItem.Enabled = OperationManager.CanUndo;
            };

            var menu = new MenuStrip()
            {
                BackColor = Color.White,
                RenderMode = ToolStripRenderMode.Professional
            };

            var lanemenu = new ToolStripMenuItem(MainFormStrings.LaneMenu, null, (s, e) =>
            {
                var form = new LaneSelectionForm() { LanesCount = noteView.LanesCount, MinusLanesCount = noteView.MinusLanesCount };
                if (form.ShowDialog(this) == DialogResult.OK)
                {
                    ApplicationSettings.Default.LanesCount = form.LanesCount;
                    noteView.LanesCount = form.LanesCount;
                    noteView.MinusLanesCount = form.MinusLanesCount;
                }
            });


            menu.Items.AddRange(new ToolStripItem[]
            {
                new ToolStripMenuItem(MainFormStrings.FileMenu, null, fileMenuItems),
                new ToolStripMenuItem(MainFormStrings.EditMenu, null, editMenuItems),
                new ToolStripMenuItem(MainFormStrings.ViewMenu, null, viewMenuItems),
                new ToolStripMenuItem(MainFormStrings.InsertMenu, null, insertMenuItems),
                // PreviewManager初期化後じゃないといけないのダメ設計でしょ
                new ToolStripMenuItem(MainFormStrings.PlayMenu, null, playMenuItems) { Enabled = PreviewManager.IsSupported },
                new ToolStripMenuItem(MainFormStrings.HelpMenu, null, helpMenuItems),
                new ToolStripMenuItem(MainFormStrings.ThemeMenu, null, themeMenuItems),
                new ToolStripMenuItem(MainFormStrings.ChannelMenu, null, channelMenuItems),
                
                
            });
            return menu;
        }

        private ToolStrip CreateMainToolStrip(NoteView noteView)
        {
            var shortcutItemBuilder = new ToolStripButtonBuilder(ShortcutManager);

            var newFileButton = shortcutItemBuilder.BuildItem(Commands.NewFile, MainFormStrings.NewFile, Resources.NewFileIcon);
            var openFileButton = shortcutItemBuilder.BuildItem(Commands.OpenFile, MainFormStrings.OpenFile, Resources.OpenFileIcon);
            var saveFileButton = shortcutItemBuilder.BuildItem(Commands.Save, MainFormStrings.SaveFile, Resources.SaveFileIcon);
            var exportButton = shortcutItemBuilder.BuildItem(Commands.ReExport, MainFormStrings.Export, Resources.ExportIcon);

            var cutButton = shortcutItemBuilder.BuildItem(Commands.Cut, MainFormStrings.Cut, Resources.CutIcon);
            var copyButton = shortcutItemBuilder.BuildItem(Commands.Copy, MainFormStrings.Copy, Resources.CopyIcon);
            var pasteButton = shortcutItemBuilder.BuildItem(Commands.Paste, MainFormStrings.Paste, Resources.PasteIcon);

            var undoButton = shortcutItemBuilder.BuildItem(Commands.Undo, MainFormStrings.Undo, Resources.UndoIcon);
            undoButton.Enabled = false;
            var redoButton = shortcutItemBuilder.BuildItem(Commands.Redo, MainFormStrings.Redo, Resources.RedoIcon);
            redoButton.Enabled = false;

            var penButton = shortcutItemBuilder.BuildItem(Commands.SelectPen, MainFormStrings.Pen, Resources.EditIcon);
            var selectionButton = shortcutItemBuilder.BuildItem(Commands.SelectSelection, MainFormStrings.Selection, Resources.SelectionIcon);
            var eraserButton = shortcutItemBuilder.BuildItem(Commands.SelectEraser, MainFormStrings.Eraser, Resources.EraserIcon);

            var zoomInButton = shortcutItemBuilder.BuildItem(Commands.ZoomIn, MainFormStrings.ZoomIn, Resources.ZoomInIcon);
            zoomInButton.Enabled = CanZoomIn;

            var zoomOutButton = shortcutItemBuilder.BuildItem(Commands.ZoomOut, MainFormStrings.ZoomOut, Resources.ZoomOutIcon);
            zoomOutButton.Enabled = CanZoomOut;

            NoteView.UnitBeatHeightChanged += (s, e) =>
            {
                zoomOutButton.Enabled = CanZoomOut;
                zoomInButton.Enabled = CanZoomIn;
                
                if (noteView.UnitBeatHeight > 240)
                {
                    if(noteView.UnitBeatHeight >= 960)
                    NoteViewScrollBar.SmallChange = 60;
                    else
                    NoteViewScrollBar.SmallChange = 120;
                }
                else
                {
                    NoteViewScrollBar.SmallChange = 480;
                }
            };

            OperationManager.OperationHistoryChanged += (s, e) =>
            {
                undoButton.Enabled = OperationManager.CanUndo;
                redoButton.Enabled = OperationManager.CanRedo;
                
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
            var shortcutItemBuilder = new ToolStripButtonBuilder(ShortcutManager);

            var tapButton = shortcutItemBuilder.BuildItem(Commands.SelectTap, "TAP", Resources.TapIcon);
            var exTapButton = shortcutItemBuilder.BuildItem(Commands.SelectExTap, "ExTAP", Resources.ExTapIcon);
            var holdButton = shortcutItemBuilder.BuildItem(Commands.SelectHold, "HOLD", Resources.HoldIcon);
            var slideButton = shortcutItemBuilder.BuildItem(Commands.SelectSlide, "SLIDE", Resources.SlideIcon);
            var slideStepButton = shortcutItemBuilder.BuildItem(Commands.SelectSlideStep, MainFormStrings.SlideStep, Resources.SlideStepIcon);
            var airActionButton = shortcutItemBuilder.BuildItem(Commands.SelectAirAction, "AIR-ACTION", Resources.AirActionIcon);
            var flickButton = shortcutItemBuilder.BuildItem(Commands.SelectFlick, "FLICK", Resources.FlickIcon);
            var damageButton = shortcutItemBuilder.BuildItem(Commands.SelectDamage, "DAMAGE", Resources.DamgeIcon);
            var guideButton = shortcutItemBuilder.BuildItem(Commands.SelectGuide, "GUIDE", Resources.GuideIcon);
            var guideStepButton = shortcutItemBuilder.BuildItem(Commands.SelectGuideStep, "GUIDESTEP", Resources.GuideStepIcon);
            var tap2Button = shortcutItemBuilder.BuildItem(Commands.SelectTap2, "TAP2", Resources.TapIcon2);
            var exTap2Button = shortcutItemBuilder.BuildItem(Commands.SelectExTap2, "ExTAP2", Resources.ExTapIcon2);

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
            ShortcutManager.ShortcutUpdated += (s, e) =>
            {
                if (ShortcutManager.ResolveShortcutKey(Commands.SelectAir, out Keys key))
                {
                    airKind.Text = $"AIR ({key.ToShortcutChar()})";
                    return;
                }
                airKind.Text = "AIR";
            };

            var quantizeTicks = new int[]
            {
                4, 8, 12, 16, 24, 32, 48, 64, 96, 128, 144, 192, 240, 256, 384, 480, 512, 576, 768, 960, 1024, 1152, 1920
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
                tapButton.Checked = noteView.NewNoteType.HasFlag(NoteType.Tap) && !noteView.IsNewNoteStart;
                exTapButton.Checked = noteView.NewNoteType.HasFlag(NoteType.ExTap) && !noteView.IsNewNoteStart;
                holdButton.Checked = noteView.NewNoteType.HasFlag(NoteType.Hold);
                slideButton.Checked = noteView.NewNoteType.HasFlag(NoteType.Slide) && !noteView.IsNewSlideStepVisible;
                slideStepButton.Checked = noteView.NewNoteType.HasFlag(NoteType.Slide) && noteView.IsNewSlideStepVisible;
                airKind.Checked = noteView.NewNoteType.HasFlag(NoteType.Air);
                airActionButton.Checked = noteView.NewNoteType.HasFlag(NoteType.AirAction);
                flickButton.Checked = noteView.NewNoteType.HasFlag(NoteType.Flick);
                damageButton.Checked = noteView.NewNoteType.HasFlag(NoteType.Damage);
                guideButton.Checked = noteView.NewNoteType.HasFlag(NoteType.Guide) && !noteView.IsNewGuideStepVisible;
                guideStepButton.Checked = noteView.NewNoteType.HasFlag(NoteType.Slide) && noteView.IsNewGuideStepVisible;
                tap2Button.Checked = noteView.NewNoteType.HasFlag(NoteType.Tap) && noteView.IsNewNoteStart;
                exTap2Button.Checked = noteView.NewNoteType.HasFlag(NoteType.ExTap) && noteView.IsNewNoteStart;
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

            


            var speedchCounts = new int[]
            {
                0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10
            };


            var speedChBox = new ToolStripComboBox("ハイスピードチャンネル")
            {
                DropDownStyle = ComboBoxStyle.DropDownList,
                AutoSize = false,
                Width = 60
            };
            speedChBox.Items.AddRange(speedchCounts.Select(p => "Ch" + p).ToArray());
            speedChBox.Items.Add(MainFormStrings.Custom);

            speedChBox.SelectedIndexChanged += (s, e) =>
            {
                if (speedChBox.SelectedIndex == speedChBox.Items.Count - 1)
                {
                    var form = new CustomChSelectionForm();
                    if (form.ShowDialog(this) == DialogResult.OK)
                    {
                        Channel = form.SpeedCh;
                        
                    }
                }
                else
                {
                    Channel = speedchCounts[speedChBox.SelectedIndex];
                }
                noteView.Channel = Channel;
                noteView.Update();
                noteView.Focus();
            };
            speedChBox.SelectedIndex = 0;


            var viewchCounts = new int[]
            {
                0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10
            };


            var viewChBox = new ToolStripComboBox("表示チャンネル")
            {
                DropDownStyle = ComboBoxStyle.DropDownList,
                AutoSize = false,
                Width = 60
            };
            viewChBox.Items.Add(MainFormStrings.All);
            viewChBox.Items.AddRange(viewchCounts.Select(p => "Ch" + p).ToArray());
            viewChBox.Items.Add(MainFormStrings.Custom);

            viewChBox.SelectedIndexChanged += (s, e) =>
            {
                if (viewChBox.SelectedIndex == viewChBox.Items.Count - 1)
                {
                    var form = new CustomViewChSelectionForm();
                    if (form.ShowDialog(this) == DialogResult.OK)
                    {
                        ViewChannel = form.SpeedCh;

                    }
                }
                else if (viewChBox.SelectedIndex == 0)
                {
                    ViewChannel = 1000;
                }
                else
                {
                    Console.WriteLine(viewchCounts[viewChBox.SelectedIndex - 1]);
                    ViewChannel = viewchCounts[viewChBox.SelectedIndex - 1];
                }
                noteView.ViewChannel = ViewChannel;
                noteView.Update();
                noteView.Focus();
            };
            viewChBox.SelectedIndex = 0;


            var widthAmountCounts = new float[]
            {
                1, 0.9f, 0.8f, 0.7f, 0.6f, 0.5f, 0.4f, 0.3f, 0.2f, 0.1f
            };


            var widthAmountBox = new ToolStripComboBox("幅変化量")
            {
                DropDownStyle = ComboBoxStyle.DropDownList,
                AutoSize = false,
                Width = 60
            };
            widthAmountBox.Items.AddRange(widthAmountCounts.Select(p => "" + p).ToArray());

            widthAmountBox.SelectedIndexChanged += (s, e) =>
            {

                    WidthAmount= widthAmountCounts[widthAmountBox.SelectedIndex];
                noteView.WidthAmount = WidthAmount;
                noteView.Update();
                noteView.Focus();
            };
            widthAmountBox.SelectedIndex = 0;

            ToolStripMenuItem laneVisible = new ToolStripMenuItem(MainFormStrings.LaneVisual, null, (s, e) =>
            {
                var item = s as ToolStripMenuItem;
                item.Checked = !item.Checked;
                NoteView.LaneVisual = item.Checked;
                item.Text = MainFormStrings.LaneVisual + ": " + item.Checked;
            })
            {
                Checked = false
            };

            /*
            var laneVisible2 = new CheckableToolStripSplitButton()
            {
                DisplayStyle = ToolStripItemDisplayStyle.Text
            };
            laneVisible2.Text = "レーン表示";
            laneVisible2.DropDown.Items.AddRange(new ToolStripItem[]
            {
                laneVisible

            });
            */





            return new ToolStrip(new ToolStripItem[]
            {
                tapButton, exTapButton, holdButton, slideButton, slideStepButton, airKind, airActionButton, flickButton, damageButton, guideButton,
                 guideStepButton, tap2Button, exTap2Button,
                quantizeComboBox, new ToolStripSeparator(), speedChBox, viewChBox,  laneVisible, widthAmountBox
            });
        }
    }
}
