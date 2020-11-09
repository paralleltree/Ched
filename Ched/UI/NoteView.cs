using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Reactive.Linq;
using System.Reactive.Disposables;

using Ched.Core;
using Ched.Core.Notes;
using Ched.Drawing;
using Ched.UI.Operations;

namespace Ched.UI
{
    public partial class NoteView : Control
    {
        public event EventHandler HeadTickChanged;
        public event EventHandler EditModeChanged;
        public event EventHandler SelectedRangeChanged;
        public event EventHandler NewNoteTypeChanged;
        public event EventHandler AirDirectionChanged;
        public event EventHandler DragScroll;

        private Color barLineColor = Color.FromArgb(160, 160, 160);
        private Color beatLineColor = Color.FromArgb(80, 80, 80);
        private Color laneBorderLightColor = Color.FromArgb(60, 60, 60);
        private Color laneBorderDarkColor = Color.FromArgb(30, 30, 30);
        private ColorProfile colorProfile;
        private int unitLaneWidth = 12;
        private int shortNoteHeight = 5;
        private int unitBeatTick = 480;
        private float unitBeatHeight = 120;

        private int headTick = 0;
        private bool editable = true;
        private EditMode editMode = EditMode.Edit;
        private int currentTick = 0;
        private SelectionRange selectedRange = SelectionRange.Empty;
        private NoteType newNoteType = NoteType.Tap;
        private AirDirection airDirection = new AirDirection(VerticalAirDirection.Up, HorizontalAirDirection.Center);
        private bool isNewSlideStepVisible = true;

        /// <summary>
        /// 小節の区切り線の色を設定します。
        /// </summary>
        public Color BarLineColor
        {
            get { return barLineColor; }
            set
            {
                barLineColor = value;
                Invalidate();
            }
        }

        /// <summary>
        /// 1拍のガイド線の色を設定します。
        /// </summary>
        public Color BeatLineColor
        {
            get { return beatLineColor; }
            set
            {
                beatLineColor = value;
                Invalidate();
            }
        }

        /// <summary>
        /// レーンのガイド線のメインカラーを設定します。
        /// </summary>
        public Color LaneBorderLightColor
        {
            get { return laneBorderLightColor; }
            set
            {
                laneBorderLightColor = value;
                Invalidate();
            }
        }

        /// <summary>
        /// レーンのガイド線のサブカラーを設定します。
        /// </summary>
        public Color LaneBorderDarkColor
        {
            get { return laneBorderDarkColor; }
            set
            {
                laneBorderDarkColor = value;
                Invalidate();
            }
        }

        /// <summary>
        /// ノーツの描画に利用する<see cref="Ched.Drawing.ColorProfile"/>を取得します。
        /// </summary>
        public ColorProfile ColorProfile
        {
            get { return colorProfile; }
        }

        /// <summary>
        /// 1レーンあたりの表示幅を設定します。
        /// </summary>
        public int UnitLaneWidth
        {
            get { return unitLaneWidth; }
            set
            {
                unitLaneWidth = value;
                Invalidate();
            }
        }

        /// <summary>
        /// レーンの表示幅を取得します。
        /// </summary>
        public int LaneWidth
        {
            get { return UnitLaneWidth * Constants.LanesCount + BorderThickness * (Constants.LanesCount - 1); }
        }

        /// <summary>
        /// レーンのガイド線の幅を取得します。
        /// </summary>
        public int BorderThickness => UnitLaneWidth < 5 ? 0 : 1;

        /// <summary>
        /// ショートノーツの表示高さを設定します。
        /// </summary>
        public int ShortNoteHeight
        {
            get { return shortNoteHeight; }
            set
            {
                shortNoteHeight = value;
                Invalidate();
            }
        }

        /// <summary>
        /// 1拍あたりのTick数を設定します。
        /// </summary>
        public int UnitBeatTick
        {
            get { return unitBeatTick; }
            set
            {
                unitBeatTick = value;
                Invalidate();
            }
        }

        /// <summary>
        /// 1拍あたりの表示高さを設定します。
        /// </summary>
        public float UnitBeatHeight
        {
            get { return unitBeatHeight; }
            set
            {
                // 6の倍数でいい感じに描画してくれる
                unitBeatHeight = value;
                Invalidate();
            }
        }

        /// <summary>
        /// クォンタイズを行うTick数を指定します。
        /// </summary>
        public double QuantizeTick { get; set; }

        /// <summary>
        /// 表示始端のTickを設定します。
        /// </summary>
        public int HeadTick
        {
            get { return headTick; }
            set
            {
                if (headTick == value) return;
                headTick = value;
                HeadTickChanged?.Invoke(this, EventArgs.Empty);
                Invalidate();
            }
        }

        /// <summary>
        /// 表示終端のTickを取得します。
        /// </summary>
        public int TailTick
        {
            get { return HeadTick + (int)(ClientSize.Height * UnitBeatTick / UnitBeatHeight); }
        }

        /// <summary>
        /// 譜面始端の表示余白に充てるTickを取得します。
        /// </summary>
        public int PaddingHeadTick
        {
            get { return UnitBeatTick / 8; }
        }

        /// <summary>
        /// ノーツが編集可能かどうかを示す値を設定します。
        /// </summary>
        public bool Editable
        {
            get { return editable; }
            set
            {
                editable = value;
                Cursor = value ? Cursors.Default : Cursors.No;
            }
        }

        /// <summary>
        /// 編集モードを設定します。
        /// </summary>
        public EditMode EditMode
        {
            get { return editMode; }
            set
            {
                editMode = value;
                EditModeChanged?.Invoke(this, EventArgs.Empty);
            }
        }

        /// <summary>
        /// 現在のTickを設定します。
        /// </summary>
        public int CurrentTick
        {
            get { return currentTick; }
            set
            {
                currentTick = value;
                if (currentTick < HeadTick || currentTick > TailTick)
                {
                    HeadTick = currentTick;
                    DragScroll?.Invoke(this, EventArgs.Empty);
                }
                Invalidate();
            }
        }

        /// <summary>
        /// 現在の選択範囲を設定します。
        /// </summary>
        public SelectionRange SelectedRange
        {
            get { return selectedRange; }
            set
            {
                selectedRange = value;
                SelectedRangeChanged?.Invoke(this, EventArgs.Empty);
                Invalidate();
            }
        }

        /// <summary>
        /// 追加するノート種別を設定します。
        /// </summary>
        public NoteType NewNoteType
        {
            get { return newNoteType; }
            set
            {
                int bits = (int)value;
                bool isSingle = bits != 0 && (bits & (bits - 1)) == 0;
                if (!isSingle) throw new ArgumentException("value", "value must be single bit.");
                newNoteType = value;
                NewNoteTypeChanged?.Invoke(this, EventArgs.Empty);
            }
        }

        /// <summary>
        /// 新たに追加するSlideのStepノートの可視性を設定します。
        /// </summary>
        public bool IsNewSlideStepVisible
        {
            get { return isNewSlideStepVisible; }
            set
            {
                isNewSlideStepVisible = value;
                NewNoteTypeChanged?.Invoke(this, EventArgs.Empty);
            }
        }

        /// <summary>
        /// 追加するAIRの方向を設定します。
        /// </summary>
        public AirDirection AirDirection
        {
            get { return airDirection; }
            set
            {
                airDirection = value;
                AirDirectionChanged?.Invoke(this, EventArgs.Empty);
            }
        }

        /// <summary>
        /// AIR-ACTION挿入時に未追加のAIRを追加するかどうか指定します。
        /// </summary>
        public bool InsertAirWithAirAction { get; set; }

        /// <summary>
        /// ノート幅に対するノート端の当たり判定に含める割合を設定します。
        /// </summary>
        public float EdgeHitWidthRate { get; set; } = 0.2f;

        /// <summary>
        /// ノート端の当たり判定幅の下限を取得します。
        /// </summary>
        public float MinimumEdgeHitWidth => UnitLaneWidth * 0.4f;

        protected int LastWidth { get; set; } = 4;

        public bool CanUndo { get { return OperationManager.CanUndo; } }

        public bool CanRedo { get { return OperationManager.CanRedo; } }

        public NoteCollection Notes { get; private set; } = new NoteCollection(new Core.NoteCollection());

        public EventCollection ScoreEvents { get; set; } = new EventCollection();

        protected OperationManager OperationManager { get; }

        protected CompositeDisposable Subscriptions { get; } = new CompositeDisposable();

        private Dictionary<Score, NoteCollection> NoteCollectionCache { get; } = new Dictionary<Score, NoteCollection>();

        public NoteView(OperationManager manager)
        {
            InitializeComponent();
            this.DoubleBuffered = true;
            this.BackColor = Color.Black;
            this.SetStyle(ControlStyles.ResizeRedraw, true);
            this.SetStyle(ControlStyles.Opaque, true);

            OperationManager = manager;

            QuantizeTick = UnitBeatTick;

            colorProfile = new ColorProfile()
            {
                BorderColor = new GradientColor(Color.FromArgb(160, 160, 160), Color.FromArgb(208, 208, 208)),
                TapColor = new GradientColor(Color.FromArgb(138, 0, 0), Color.FromArgb(255, 128, 128)),
                ExTapColor = new GradientColor(Color.FromArgb(204, 192, 0), Color.FromArgb(255, 236, 68)),
                FlickColor = Tuple.Create(new GradientColor(Color.FromArgb(68, 68, 68), Color.FromArgb(186, 186, 186)), new GradientColor(Color.FromArgb(0, 96, 138), Color.FromArgb(122, 216, 252))),
                DamageColor = new GradientColor(Color.FromArgb(8, 8, 116), Color.FromArgb(22, 40, 180)),
                HoldColor = new GradientColor(Color.FromArgb(196, 86, 0), Color.FromArgb(244, 156, 102)),
                HoldBackgroundColor = new GradientColor(Color.FromArgb(196, 166, 44, 168), Color.FromArgb(196, 216, 216, 0)),
                SlideColor = new GradientColor(Color.FromArgb(0, 16, 138), Color.FromArgb(86, 106, 255)),
                SlideLineColor = Color.FromArgb(196, 0, 214, 192),
                SlideBackgroundColor = new GradientColor(Color.FromArgb(196, 166, 44, 168), Color.FromArgb(196, 0, 164, 146)),
                AirUpColor = Color.FromArgb(28, 206, 22),
                AirDownColor = Color.FromArgb(192, 21, 216),
                AirActionColor = new GradientColor(Color.FromArgb(146, 0, 192), Color.FromArgb(212, 92, 255)),
                AirHoldLineColor = Color.FromArgb(216, 0, 196, 0),
                AirStepColor = new GradientColor(Color.FromArgb(6, 180, 10), Color.FromArgb(80, 224, 64))
            };

            mouseDown = this.MouseDownAsObservable();
            mouseMove = this.MouseMoveAsObservable();
            mouseUp = this.MouseUpAsObservable();
            InitializeHandlers();
        }


        private Matrix GetDrawingMatrix(Matrix baseMatrix)
        {
            return GetDrawingMatrix(baseMatrix, true);
        }

        private Matrix GetDrawingMatrix(Matrix baseMatrix, bool flipY)
        {
            Matrix matrix = baseMatrix.Clone();
            if (flipY)
            {
                // 反転してY軸増加方向を時間軸に
                matrix.Scale(1, -1);
            }
            // ずれたコントロール高さ分を補正
            matrix.Translate(0, ClientSize.Height - 1, MatrixOrder.Append);
            // さらにずらして下端とHeadTickを合わせる
            matrix.Translate(0, HeadTick * UnitBeatHeight / UnitBeatTick, MatrixOrder.Append);
            // 水平方向に対して中央に寄せる
            matrix.Translate((ClientSize.Width - LaneWidth) / 2, 0);

            return matrix;
        }

        private float GetYPositionFromTick(int tick)
        {
            return tick * UnitBeatHeight / UnitBeatTick;
        }

        protected int GetTickFromYPosition(float y)
        {
            return (int)(y * UnitBeatTick / UnitBeatHeight);
        }

        protected int GetQuantizedTick(int tick)
        {
            var sigs = ScoreEvents.TimeSignatureChangeEvents.OrderBy(p => p.Tick).ToList();

            int head = 0;
            for (int i = 0; i < sigs.Count; i++)
            {
                int barTick = UnitBeatTick * 4 * sigs[i].Numerator / sigs[i].Denominator;

                if (i < sigs.Count - 1)
                {
                    int nextHead = head + (sigs[i + 1].Tick - head) / barTick * barTick;
                    if (tick >= nextHead)
                    {
                        head = nextHead;
                        continue;
                    }
                }

                int headBarTick = head + (tick - head) / barTick * barTick;
                int offsetCount = (int)Math.Round((float)(tick - headBarTick) / QuantizeTick);
                int maxOffsetCount = (int)(barTick / QuantizeTick);
                int remnantTick = barTick - (int)(maxOffsetCount * QuantizeTick);
                return headBarTick + ((tick - headBarTick >= barTick - remnantTick / 2) ? barTick : (int)(offsetCount * QuantizeTick));
            }

            throw new InvalidOperationException();
        }

        private RectangleF GetRectFromNotePosition(int tick, int laneIndex, int width)
        {
            return new RectangleF(
                (UnitLaneWidth + BorderThickness) * laneIndex + BorderThickness,
                GetYPositionFromTick(tick) - ShortNoteHeight / 2,
                (UnitLaneWidth + BorderThickness) * width - BorderThickness,
                ShortNoteHeight
                );
        }

        private RectangleF GetClickableRectFromNotePosition(int tick, int laneIndex, int width)
        {
            return GetRectFromNotePosition(tick, laneIndex, width).Expand(1);
        }

        private Rectangle GetSelectionRect()
        {
            int minTick = SelectedRange.Duration < 0 ? SelectedRange.StartTick + SelectedRange.Duration : SelectedRange.StartTick;
            int maxTick = SelectedRange.Duration < 0 ? SelectedRange.StartTick : SelectedRange.StartTick + SelectedRange.Duration;
            var start = new Point(SelectedRange.StartLaneIndex * (UnitLaneWidth + BorderThickness), (int)GetYPositionFromTick(minTick) - ShortNoteHeight);
            var end = new Point((SelectedRange.StartLaneIndex + SelectedRange.SelectedLanesCount) * (UnitLaneWidth + BorderThickness), (int)GetYPositionFromTick(maxTick) + ShortNoteHeight);
            return new Rectangle(start.X, start.Y, end.X - start.X, end.Y - start.Y);
        }

        protected void DrawSelectionRange(Graphics g)
        {
            Rectangle selectedRect = GetSelectionRect();
            g.DrawXorRectangle(PenStyles.Dot, g.Transform.TransformPoint(selectedRect.Location), g.Transform.TransformPoint(selectedRect.Location + selectedRect.Size));
        }

        public Core.NoteCollection GetSelectedNotes()
        {
            int minTick = SelectedRange.StartTick + (SelectedRange.Duration < 0 ? SelectedRange.Duration : 0);
            int maxTick = SelectedRange.StartTick + (SelectedRange.Duration < 0 ? 0 : SelectedRange.Duration);
            int startLaneIndex = SelectedRange.StartLaneIndex;
            int endLaneIndex = SelectedRange.StartLaneIndex + SelectedRange.SelectedLanesCount;

            var c = new Core.NoteCollection();

            bool contained(IAirable p) => p.Tick >= minTick && p.Tick <= maxTick & p.LaneIndex >= startLaneIndex && p.LaneIndex + p.Width <= endLaneIndex;
            c.Taps.AddRange(Notes.Taps.Where(p => contained(p)));
            c.ExTaps.AddRange(Notes.ExTaps.Where(p => contained(p)));
            c.Flicks.AddRange(Notes.Flicks.Where(p => contained(p)));
            c.Damages.AddRange(Notes.Damages.Where(p => contained(p)));
            c.Holds.AddRange(Notes.Holds.Where(p => p.StartTick >= minTick && p.StartTick + p.Duration <= maxTick && p.LaneIndex >= startLaneIndex && p.LaneIndex + p.Width <= endLaneIndex));
            c.Slides.AddRange(Notes.Slides.Where(p => p.StartTick >= minTick && p.StartTick + p.GetDuration() <= maxTick && p.StartLaneIndex >= startLaneIndex && p.StartLaneIndex + p.StartWidth <= endLaneIndex && p.StepNotes.All(r => r.LaneIndex >= startLaneIndex && r.LaneIndex + r.Width <= endLaneIndex)));

            var airables = c.GetShortNotes().Cast<IAirable>()
                .Concat(c.Holds.Select(p => p.EndNote))
                .Concat(c.Slides.SelectMany(p => p.StepNotes))
                .ToList();
            c.Airs.AddRange(airables.SelectMany(p => Notes.GetReferencedAir(p)));
            // AIR-ACTIONはとりあえず全コピー
            c.AirActions.AddRange(airables.SelectMany(p => Notes.GetReferencedAirAction(p)));
            return c;
        }

        public void SelectAll()
        {
            SelectedRange = new SelectionRange()
            {
                StartTick = 0,
                Duration = Notes.GetLastTick(),
                StartLaneIndex = 0,
                SelectedLanesCount = Constants.LanesCount
            };
        }

        public void SelectToEnd()
        {
            SelectedRange = new SelectionRange()
            {
                StartTick = CurrentTick,
                Duration = Notes.GetLastTick() - CurrentTick,
                StartLaneIndex = 0,
                SelectedLanesCount = Constants.LanesCount
            };
        }

        public void SelectToBeginning()
        {
            SelectedRange = new SelectionRange()
            {
                StartTick = 0,
                Duration = CurrentTick,
                StartLaneIndex = 0,
                SelectedLanesCount = Constants.LanesCount
            };
        }

        public void CutSelectedNotes()
        {
            CopySelectedNotes();
            RemoveSelectedNotes();
        }

        public void CopySelectedNotes()
        {
            var data = new SelectionData(SelectedRange.StartTick + Math.Min(SelectedRange.Duration, 0), UnitBeatTick, GetSelectedNotes());
            Clipboard.SetDataObject(data, true);
        }

        public void PasteNotes()
        {
            var op = PasteNotes(p => { });
            if (op == null) return;
            OperationManager.Push(op);
            Invalidate();
        }

        public void PasteFlippedNotes()
        {
            var op = PasteNotes(p => FlipNotes(p.SelectedNotes));
            if (op == null) return;
            OperationManager.Push(op);
            Invalidate();
        }

        /// <summary>
        /// クリップボードにコピーされたノーツをペーストしてその操作を表す<see cref="IOperation"/>を返します。
        /// ペーストするノーツがない場合はnullを返します。
        /// </summary>
        /// <param name="action">選択データに対して適用するアクション</param>
        /// <returns>ペースト操作を表す<see cref="IOperation"/></returns>
        protected IOperation PasteNotes(Action<SelectionData> action)
        {
            var obj = Clipboard.GetDataObject();
            if (obj == null || !obj.GetDataPresent(typeof(SelectionData))) return null;

            var data = obj.GetData(typeof(SelectionData)) as SelectionData;
            if (data.IsEmpty) return null;

            double tickFactor = UnitBeatTick / (double)data.TicksPerBeat;
            int originTick = (int)(data.StartTick * tickFactor);
            if (data.TicksPerBeat != UnitBeatTick)
                data.SelectedNotes.UpdateTicksPerBeat(tickFactor);

            foreach (var note in data.SelectedNotes.GetShortNotes())
            {
                note.Tick = note.Tick - originTick + CurrentTick;
            }

            foreach (var hold in data.SelectedNotes.Holds)
            {
                hold.StartTick = hold.StartTick - originTick + CurrentTick;
            }

            foreach (var slide in data.SelectedNotes.Slides)
            {
                slide.StartTick = slide.StartTick - originTick + CurrentTick;
            }

            foreach (var airAction in data.SelectedNotes.AirActions)
            {
                // AIR-ACTIONの親ノート復元できないんやった……クソ設計だわ……
                var notes = airAction.ActionNotes.Select(p => new AirAction.ActionNote(airAction) { Offset = p.Offset }).ToList();
                airAction.ActionNotes.Clear();
                airAction.ActionNotes.AddRange(notes);
            }

            action(data);

            var op = data.SelectedNotes.Taps.Select(p => new InsertTapOperation(Notes, p)).Cast<IOperation>()
                .Concat(data.SelectedNotes.ExTaps.Select(p => new InsertExTapOperation(Notes, p)))
                .Concat(data.SelectedNotes.Flicks.Select(p => new InsertFlickOperation(Notes, p)))
                .Concat(data.SelectedNotes.Damages.Select(p => new InsertDamageOperation(Notes, p)))
                .Concat(data.SelectedNotes.Holds.Select(p => new InsertHoldOperation(Notes, p)))
                .Concat(data.SelectedNotes.Slides.Select(p => new InsertSlideOperation(Notes, p)))
                .Concat(data.SelectedNotes.Airs.Select(p => new InsertAirOperation(Notes, p)))
                .Concat(data.SelectedNotes.AirActions.Select(p => new InsertAirActionOperation(Notes, p)));
            var composite = new CompositeOperation("クリップボードからペースト", op.ToList());
            composite.Redo(); // 追加書くの面倒になったので許せ
            return composite;
        }

        public void RemoveSelectedNotes()
        {
            var selected = GetSelectedNotes();

            var airs = selected.Airs.ToList().Select(p =>
            {
                Notes.Remove(p);
                return new RemoveAirOperation(Notes, p);
            });
            var airActions = selected.AirActions.Select(p =>
            {
                Notes.Remove(p);
                return new RemoveAirActionOperation(Notes, p);
            }).ToList();

            var taps = selected.Taps.Select(p =>
            {
                Notes.Remove(p);
                return new RemoveTapOperation(Notes, p);
            });
            var extaps = selected.ExTaps.Select(p =>
            {
                Notes.Remove(p);
                return new RemoveExTapOperation(Notes, p);
            });
            var flicks = selected.Flicks.Select(p =>
            {
                Notes.Remove(p);
                return new RemoveFlickOperation(Notes, p);
            });
            var damages = selected.Damages.Select(p =>
            {
                Notes.Remove(p);
                return new RemoveDamageOperation(Notes, p);
            });
            var holds = selected.Holds.Select(p =>
            {
                Notes.Remove(p);
                return new RemoveHoldOperation(Notes, p);
            });
            var slides = selected.Slides.Select(p =>
            {
                Notes.Remove(p);
                return new RemoveSlideOperation(Notes, p);
            });

            var opList = taps.Cast<IOperation>().Concat(extaps).Concat(flicks).Concat(damages)
                .Concat(holds).Concat(slides)
                .Concat(airs).Concat(airActions)
                .ToList();

            if (opList.Count == 0) return;
            OperationManager.Push(new CompositeOperation("選択範囲内ノーツ削除", opList));
            Invalidate();
        }

        public void FlipSelectedNotes()
        {
            var op = FlipNotes(GetSelectedNotes());
            if (op == null) return;
            OperationManager.Push(op);
            Invalidate();
        }

        /// <summary>
        /// 指定のコレクション内のノーツを反転してその操作を表す<see cref="IOperation"/>を返します。
        /// 反転するノーツがない場合はnullを返します。
        /// </summary>
        /// <param name="notes">反転対象となるノーツを含む<see cref="Core.NoteCollection"/></param>
        /// <returns>反転操作を表す<see cref="IOperation"/></returns>
        protected IOperation FlipNotes(Core.NoteCollection notes)
        {
            var dicShortNotes = notes.GetShortNotes().ToDictionary(q => q, q => new MoveShortNoteOperation.NotePosition(q.Tick, q.LaneIndex));
            var dicHolds = notes.Holds.ToDictionary(q => q, q => new MoveHoldOperation.NotePosition(q.StartTick, q.LaneIndex, q.Width));
            var dicSlides = notes.Slides;
            var referenced = new NoteCollection(notes);
            var airs = notes.GetShortNotes().Cast<IAirable>()
                .Concat(notes.Holds.Select(p => p.EndNote))
                .Concat(notes.Slides.Select(p => p.StepNotes.OrderByDescending(q => q.TickOffset).First()))
                .SelectMany(p => referenced.GetReferencedAir(p));

            var opShortNotes = dicShortNotes.Select(p =>
            {
                p.Key.LaneIndex = Constants.LanesCount - p.Key.LaneIndex - p.Key.Width;
                var after = new MoveShortNoteOperation.NotePosition(p.Key.Tick, p.Key.LaneIndex);
                return new MoveShortNoteOperation(p.Key, p.Value, after);
            });

            var opHolds = dicHolds.Select(p =>
            {
                p.Key.LaneIndex = Constants.LanesCount - p.Key.LaneIndex - p.Key.Width;
                var after = new MoveHoldOperation.NotePosition(p.Key.StartTick, p.Key.LaneIndex, p.Key.Width);
                return new MoveHoldOperation(p.Key, p.Value, after);
            });

            var opSlides = dicSlides.Select(p =>
            {
                p.Flip();
                return new FlipSlideOperation(p);
            });

            var opAirs = airs.Select(p =>
            {
                p.Flip();
                return new FlipAirHorizontalDirectionOperation(p);
            });

            var opList = opShortNotes.Cast<IOperation>().Concat(opHolds).Concat(opSlides).Concat(opAirs).ToList();
            return opList.Count == 0 ? null : new CompositeOperation("ノーツの反転", opList);
        }

        public void Undo()
        {
            if (!OperationManager.CanUndo) return;
            OperationManager.Undo();
            Invalidate();
        }

        public void Redo()
        {
            if (!OperationManager.CanRedo) return;
            OperationManager.Redo();
            Invalidate();
        }


        public void Initialize()
        {
            SelectedRange = SelectionRange.Empty;
            CurrentTick = SelectedRange.StartTick;
            NoteCollectionCache.Clear();
            Invalidate();
        }

        public void Initialize(Score score)
        {
            Initialize();
            UpdateScore(score);
        }

        public void UpdateScore(Score score)
        {
            UnitBeatTick = score.TicksPerBeat;
            // Core.NoteCollectionとは別のコレクションと共にNoteView.NoteCollectionでラップする場合は
            // 異なるNoteView.NoteCollectionを生成すると要素の追加/削除時に整合性を失う
            if (NoteCollectionCache.ContainsKey(score))
            {
                Notes = NoteCollectionCache[score];
            }
            else
            {
                Notes = new NoteCollection(score.Notes);
                NoteCollectionCache.Add(score, Notes);
            }
            ScoreEvents = score.Events;
            Invalidate();
        }

        public class NoteCollection
        {
            public event EventHandler NoteChanged;

            private Core.NoteCollection source;

            private Dictionary<IAirable, Air> AirDictionary { get; } = new Dictionary<IAirable, Air>();
            private Dictionary<IAirable, AirAction> AirActionDictionary { get; } = new Dictionary<IAirable, AirAction>();

            public IReadOnlyCollection<Tap> Taps { get { return source.Taps; } }
            public IReadOnlyCollection<ExTap> ExTaps { get { return source.ExTaps; } }
            public IReadOnlyCollection<Hold> Holds { get { return source.Holds; } }
            public IReadOnlyCollection<Slide> Slides { get { return source.Slides; } }
            public IReadOnlyCollection<Air> Airs { get { return source.Airs; } }
            public IReadOnlyCollection<AirAction> AirActions { get { return source.AirActions; } }
            public IReadOnlyCollection<Flick> Flicks { get { return source.Flicks; } }
            public IReadOnlyCollection<Damage> Damages { get { return source.Damages; } }

            public NoteCollection(Core.NoteCollection src)
            {
                source = src;
                foreach (var air in src.Airs)
                {
                    AirDictionary.Add(air.ParentNote, air);
                }
                foreach (var airAction in src.AirActions)
                {
                    AirActionDictionary.Add(airAction.ParentNote, airAction);
                }
            }

            public void Add(Tap note)
            {
                source.Taps.Add(note);
                NoteChanged?.Invoke(this, EventArgs.Empty);
            }

            public void Add(ExTap note)
            {
                source.ExTaps.Add(note);
                NoteChanged?.Invoke(this, EventArgs.Empty);
            }

            public void Add(Hold note)
            {
                source.Holds.Add(note);
                NoteChanged?.Invoke(this, EventArgs.Empty);
            }

            public void Add(Slide note)
            {
                source.Slides.Add(note);
                NoteChanged?.Invoke(this, EventArgs.Empty);
            }

            public void Add(Air note)
            {
                source.Airs.Add(note);
                AirDictionary.Add(note.ParentNote, note);
                NoteChanged?.Invoke(this, EventArgs.Empty);
            }

            public void Add(AirAction note)
            {
                source.AirActions.Add(note);
                AirActionDictionary.Add(note.ParentNote, note);
                NoteChanged?.Invoke(this, EventArgs.Empty);
            }

            public void Add(Flick note)
            {
                source.Flicks.Add(note);
                NoteChanged?.Invoke(this, EventArgs.Empty);
            }

            public void Add(Damage note)
            {
                source.Damages.Add(note);
                NoteChanged?.Invoke(this, EventArgs.Empty);
            }


            public void Remove(Tap note)
            {
                source.Taps.Remove(note);
                NoteChanged?.Invoke(this, EventArgs.Empty);
            }

            public void Remove(ExTap note)
            {
                source.ExTaps.Remove(note);
                NoteChanged?.Invoke(this, EventArgs.Empty);
            }

            public void Remove(Hold note)
            {
                source.Holds.Remove(note);
                NoteChanged?.Invoke(this, EventArgs.Empty);
            }

            public void Remove(Slide note)
            {
                source.Slides.Remove(note);
                NoteChanged?.Invoke(this, EventArgs.Empty);
            }

            public void Remove(Air note)
            {
                source.Airs.Remove(note);
                AirDictionary.Remove(note.ParentNote);
                NoteChanged?.Invoke(this, EventArgs.Empty);
            }

            public void Remove(AirAction note)
            {
                source.AirActions.Remove(note);
                AirActionDictionary.Remove(note.ParentNote);
                NoteChanged?.Invoke(this, EventArgs.Empty);
            }

            public void Remove(Flick note)
            {
                source.Flicks.Remove(note);
                NoteChanged?.Invoke(this, EventArgs.Empty);
            }

            public void Remove(Damage note)
            {
                source.Damages.Remove(note);
                NoteChanged?.Invoke(this, EventArgs.Empty);
            }

            public IEnumerable<Air> GetReferencedAir(IAirable note)
            {
                if (AirDictionary.ContainsKey(note))
                    yield return AirDictionary[note];
            }

            public IEnumerable<AirAction> GetReferencedAirAction(IAirable note)
            {
                if (AirActionDictionary.ContainsKey(note))
                    yield return AirActionDictionary[note];
            }

            public int GetLastTick()
            {
                var shortNotes = Taps.Cast<TappableBase>().Concat(ExTaps).Concat(Flicks).Concat(Damages).ToList();
                var longNotes = Holds.Cast<ILongNote>().Concat(Slides).Concat(AirActions).ToList();
                int lastShortNoteTick = shortNotes.Count == 0 ? 0 : shortNotes.Max(p => p.Tick);
                int lastLongNoteTick = longNotes.Count == 0 ? 0 : longNotes.Max(p => p.StartTick + p.GetDuration());
                return Math.Max(lastShortNoteTick, lastLongNoteTick);
            }


            public void UpdateTicksPerBeat(double factor)
            {
                source.UpdateTicksPerBeat(factor);
            }
        }
    }

    public enum EditMode
    {
        Select,
        Edit,
        Erase
    }

    [Flags]
    public enum NoteType
    {
        Tap = 1,
        ExTap = 1 << 1,
        Hold = 1 << 2,
        Slide = 1 << 3,
        Air = 1 << 4,
        AirAction = 1 << 5,
        Flick = 1 << 6,
        Damage = 1 << 7
    }

    public struct AirDirection
    {
        public VerticalAirDirection VerticalDirection { get; }
        public HorizontalAirDirection HorizontalDirection { get; }

        public AirDirection(VerticalAirDirection verticalDirection, HorizontalAirDirection horizontaiDirection)
        {
            VerticalDirection = verticalDirection;
            HorizontalDirection = horizontaiDirection;
        }
    }
}
