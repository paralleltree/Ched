using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Reactive.Linq;
using System.Reactive.Concurrency;
using System.Reactive.Disposables;

using Ched.Core;
using Ched.Core.Notes;
using Ched.Core.Events;
using Ched.Drawing;
using Ched.UI.Operations;

namespace Ched.UI
{
    public partial class NoteView : Control
    {
        public event EventHandler UnitLaneWidthChanged;
        public event EventHandler UnitBeatHeightChanged;
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
                UnitLaneWidthChanged?.Invoke(this, EventArgs.Empty);
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
                UnitBeatHeightChanged?.Invoke(this, EventArgs.Empty);
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

            InitializeHandlers();
        }

        private void InitializeHandlers()
        {
            var mouseDown = this.MouseDownAsObservable();
            var mouseMove = this.MouseMoveAsObservable();
            var mouseUp = this.MouseUpAsObservable();

            // マウスをクリックしているとき以外
            var mouseMoveSubscription = mouseMove.TakeUntil(mouseDown).Concat(mouseMove.SkipUntil(mouseUp).TakeUntil(mouseDown).Repeat())
                .Where(p => EditMode == EditMode.Edit && Editable)
                .Do(p =>
                {
                    var pos = GetDrawingMatrix(new Matrix()).GetInvertedMatrix().TransformPoint(p.Location);
                    int tailTick = TailTick;
                    bool visibleTick(int t) => t >= HeadTick && t <= tailTick;

                    var airActions = Notes.AirActions.Reverse()
                        .SelectMany(q => q.ActionNotes.Where(r => visibleTick(q.StartTick + r.Offset)))
                        .Select(q => GetClickableRectFromNotePosition(q.ParentNote.StartTick + q.Offset, q.ParentNote.ParentNote.LaneIndex, q.ParentNote.ParentNote.Width));

                    var shortNotes = Enumerable.Empty<TappableBase>()
                        .Concat(Notes.Damages.Reverse())
                        .Concat(Notes.ExTaps.Reverse())
                        .Concat(Notes.Taps.Reverse())
                        .Concat(Notes.Flicks.Reverse())
                        .Where(q => visibleTick(q.Tick))
                        .Select(q => GetClickableRectFromNotePosition(q.Tick, q.LaneIndex, q.Width));

                    var slides = Notes.Slides.Reverse()
                        .SelectMany(q => q.StepNotes.OrderByDescending(r => r.TickOffset).Concat(new LongNoteTapBase[] { q.StartNote }))
                        .Where(q => visibleTick(q.Tick))
                        .Select(q => GetClickableRectFromNotePosition(q.Tick, q.LaneIndex, q.Width));

                    foreach (RectangleF rect in airActions)
                    {
                        if (!rect.Contains(pos)) continue;
                        Cursor = Cursors.SizeNS;
                        return;
                    }

                    foreach (RectangleF rect in shortNotes.Concat(slides))
                    {
                        if (!rect.Contains(pos)) continue;
                        RectangleF left = rect.GetLeftThumb(EdgeHitWidthRate, MinimumEdgeHitWidth);
                        RectangleF right = rect.GetRightThumb(EdgeHitWidthRate, MinimumEdgeHitWidth);
                        Cursor = (left.Contains(pos) || right.Contains(pos)) ? Cursors.SizeWE : Cursors.SizeAll;
                        return;
                    }

                    foreach (var hold in Notes.Holds.Reverse())
                    {
                        if (GetClickableRectFromNotePosition(hold.EndNote.Tick, hold.LaneIndex, hold.Width).Contains(pos))
                        {
                            Cursor = Cursors.SizeNS;
                            return;
                        }

                        RectangleF rect = GetClickableRectFromNotePosition(hold.StartTick, hold.LaneIndex, hold.Width);
                        if (!rect.Contains(pos)) continue;
                        RectangleF left = rect.GetLeftThumb(EdgeHitWidthRate, MinimumEdgeHitWidth);
                        RectangleF right = rect.GetRightThumb(EdgeHitWidthRate, MinimumEdgeHitWidth);
                        Cursor = (left.Contains(pos) || right.Contains(pos)) ? Cursors.SizeWE : Cursors.SizeAll;
                        return;
                    }

                    Cursor = Cursors.Default;
                })
                .Subscribe();

            var dragSubscription = mouseDown
                .SelectMany(p => mouseMove.TakeUntil(mouseUp).TakeUntil(mouseUp)
                    .CombineLatest(Observable.Interval(TimeSpan.FromMilliseconds(200)).TakeUntil(mouseUp), (q, r) => q)
                    .Sample(TimeSpan.FromMilliseconds(200), new ControlScheduler(this))
                    .Do(q =>
                    {
                        // コントロール端にドラッグされたらスクロールする
                        if (q.Y <= ClientSize.Height * 0.1)
                        {
                            HeadTick += UnitBeatTick;
                            DragScroll?.Invoke(this, EventArgs.Empty);
                        }
                        else if (q.Y >= ClientSize.Height * 0.9)
                        {
                            HeadTick -= HeadTick + PaddingHeadTick < UnitBeatTick ? HeadTick + PaddingHeadTick : UnitBeatTick;
                            DragScroll?.Invoke(this, EventArgs.Empty);
                        }
                    })).Subscribe();

            var editSubscription = mouseDown
                .Where(p => Editable)
                .Where(p => p.Button == MouseButtons.Left && EditMode == EditMode.Edit)
                .SelectMany(p =>
                {
                    int tailTick = TailTick;
                    var from = p.Location;
                    Matrix matrix = GetDrawingMatrix(new Matrix());
                    matrix.Invert();
                    PointF scorePos = matrix.TransformPoint(p.Location);

                    // そもそも描画領域外であれば何もしない
                    RectangleF scoreRect = new RectangleF(0, GetYPositionFromTick(HeadTick), LaneWidth, GetYPositionFromTick(TailTick) - GetYPositionFromTick(HeadTick));
                    if (!scoreRect.Contains(scorePos)) return Observable.Empty<MouseEventArgs>();

                    IObservable<MouseEventArgs> actionNoteHandler(AirAction.ActionNote action)
                    {
                        var offsets = new HashSet<int>(action.ParentNote.ActionNotes.Select(q => q.Offset));
                        offsets.Remove(action.Offset);
                        return mouseMove
                            .TakeUntil(mouseUp)
                            .Do(q =>
                            {
                                var currentScorePos = GetDrawingMatrix(new Matrix()).GetInvertedMatrix().TransformPoint(q.Location);
                                int offset = GetQuantizedTick(GetTickFromYPosition(currentScorePos.Y)) - action.ParentNote.ParentNote.Tick;
                                if (offset <= 0 || offsets.Contains(offset)) return;
                                action.Offset = offset;
                                Cursor.Current = Cursors.SizeNS;
                            }).Finally(() => Cursor.Current = Cursors.Default);
                    }

                    // AIR-ACTION
                    foreach (var note in Notes.AirActions.Reverse())
                    {
                        foreach (var action in note.ActionNotes)
                        {
                            RectangleF noteRect = GetClickableRectFromNotePosition(note.ParentNote.Tick + action.Offset, note.ParentNote.LaneIndex, note.ParentNote.Width);
                            if (noteRect.Contains(scorePos))
                            {
                                int beforeOffset = action.Offset;
                                return actionNoteHandler(action)
                                    .Finally(() =>
                                    {
                                        if (beforeOffset == action.Offset) return;
                                        OperationManager.Push(new ChangeAirActionOffsetOperation(action, beforeOffset, action.Offset));
                                    });
                            }
                        }
                    }

                    IObservable<MouseEventArgs> moveTappableNoteHandler(TappableBase note)
                    {
                        int beforeLaneIndex = note.LaneIndex;
                        return mouseMove
                            .TakeUntil(mouseUp)
                            .Do(q =>
                            {
                                var currentScorePos = GetDrawingMatrix(new Matrix()).GetInvertedMatrix().TransformPoint(q.Location);
                                note.Tick = Math.Max(GetQuantizedTick(GetTickFromYPosition(currentScorePos.Y)), 0);
                                int xdiff = (int)((currentScorePos.X - scorePos.X) / (UnitLaneWidth + BorderThickness));
                                int laneIndex = beforeLaneIndex + xdiff;
                                note.LaneIndex = Math.Min(Constants.LanesCount - note.Width, Math.Max(0, laneIndex));
                                Cursor.Current = Cursors.SizeAll;
                            })
                            .Finally(() => Cursor.Current = Cursors.Default);
                    }

                    IObservable<MouseEventArgs> tappableNoteLeftThumbHandler(TappableBase note)
                    {
                        var beforePos = new ChangeShortNoteWidthOperation.NotePosition(note.LaneIndex, note.Width);
                        return mouseMove
                            .TakeUntil(mouseUp)
                            .Do(q =>
                            {
                                var currentScorePos = GetDrawingMatrix(new Matrix()).GetInvertedMatrix().TransformPoint(q.Location);
                                int xdiff = (int)((currentScorePos.X - scorePos.X) / (UnitLaneWidth + BorderThickness));
                                xdiff = Math.Min(beforePos.Width - 1, Math.Max(-beforePos.LaneIndex, xdiff));
                                int width = beforePos.Width - xdiff;
                                int laneIndex = beforePos.LaneIndex + xdiff;
                                width = Math.Min(Constants.LanesCount - laneIndex, Math.Max(1, width));
                                laneIndex = Math.Min(Constants.LanesCount - width, Math.Max(0, laneIndex));
                                note.SetPosition(laneIndex, width);
                                Cursor.Current = Cursors.SizeWE;
                            })
                            .Finally(() =>
                            {
                                Cursor.Current = Cursors.Default;
                                LastWidth = note.Width;
                            });
                    }

                    IObservable<MouseEventArgs> tappableNoteRightThumbHandler(TappableBase note)
                    {
                        int beforeWidth = note.Width;
                        return mouseMove
                            .TakeUntil(mouseUp)
                            .Do(q =>
                            {
                                var currentScorePos = GetDrawingMatrix(new Matrix()).GetInvertedMatrix().TransformPoint(q.Location);
                                int xdiff = (int)((currentScorePos.X - scorePos.X) / (UnitLaneWidth + BorderThickness));
                                int width = beforeWidth + xdiff;
                                note.Width = Math.Min(Constants.LanesCount - note.LaneIndex, Math.Max(1, width));
                                Cursor.Current = Cursors.SizeWE;
                            })
                            .Finally(() =>
                            {
                                Cursor.Current = Cursors.Default;
                                LastWidth = note.Width;
                            });
                    }


                    IObservable<MouseEventArgs> shortNoteHandler(TappableBase note)
                    {
                        RectangleF rect = GetClickableRectFromNotePosition(note.Tick, note.LaneIndex, note.Width);
                        // ノートの左側
                        if (rect.GetLeftThumb(EdgeHitWidthRate, MinimumEdgeHitWidth).Contains(scorePos))
                        {
                            var beforePos = new ChangeShortNoteWidthOperation.NotePosition(note.LaneIndex, note.Width);
                            return tappableNoteLeftThumbHandler(note)
                                .Finally(() =>
                                {
                                    var afterPos = new ChangeShortNoteWidthOperation.NotePosition(note.LaneIndex, note.Width);
                                    if (beforePos == afterPos) return;
                                    OperationManager.Push(new ChangeShortNoteWidthOperation(note, beforePos, afterPos));
                                });
                        }

                        // ノートの右側
                        if (rect.GetRightThumb(EdgeHitWidthRate, MinimumEdgeHitWidth).Contains(scorePos))
                        {
                            var beforePos = new ChangeShortNoteWidthOperation.NotePosition(note.LaneIndex, note.Width);
                            return tappableNoteRightThumbHandler(note)
                                .Finally(() =>
                                {
                                    var afterPos = new ChangeShortNoteWidthOperation.NotePosition(note.LaneIndex, note.Width);
                                    if (beforePos == afterPos) return;
                                    OperationManager.Push(new ChangeShortNoteWidthOperation(note, beforePos, afterPos));
                                });
                        }

                        // ノート本体
                        if (rect.Contains(scorePos))
                        {
                            var beforePos = new MoveShortNoteOperation.NotePosition(note.Tick, note.LaneIndex);
                            return moveTappableNoteHandler(note)
                                .Finally(() =>
                                {
                                    var afterPos = new MoveShortNoteOperation.NotePosition(note.Tick, note.LaneIndex);
                                    if (beforePos == afterPos) return;
                                    OperationManager.Push(new MoveShortNoteOperation(note, beforePos, afterPos));
                                });
                        }

                        return null;
                    }

                    IObservable<MouseEventArgs> holdDurationHandler(Hold hold)
                    {
                        return mouseMove.TakeUntil(mouseUp)
                            .Do(q =>
                            {
                                var currentScorePos = GetDrawingMatrix(new Matrix()).GetInvertedMatrix().TransformPoint(q.Location);
                                hold.Duration = (int)Math.Max(QuantizeTick, GetQuantizedTick(GetTickFromYPosition(currentScorePos.Y)) - hold.StartTick);
                                Cursor.Current = Cursors.SizeNS;
                            })
                            .Finally(() => Cursor.Current = Cursors.Default);
                    }

                    IObservable<MouseEventArgs> leftSlideStepNoteHandler(Slide.StepTap step)
                    {
                        var beforeStepPos = new MoveSlideStepNoteOperation.NotePosition(step.TickOffset, step.LaneIndexOffset, step.WidthChange);

                        return mouseMove
                            .TakeUntil(mouseUp)
                            .Do(q =>
                            {
                                var currentScorePos = GetDrawingMatrix(new Matrix()).GetInvertedMatrix().TransformPoint(q.Location);
                                int xdiff = (int)((currentScorePos.X - scorePos.X) / (UnitLaneWidth + BorderThickness));
                                int laneIndexOffset = beforeStepPos.LaneIndexOffset + xdiff;
                                int widthChange = beforeStepPos.WidthChange - xdiff;
                                laneIndexOffset = Math.Min(beforeStepPos.LaneIndexOffset + step.ParentNote.StartWidth + beforeStepPos.WidthChange - 1, Math.Max(-step.ParentNote.StartLaneIndex, laneIndexOffset));
                                widthChange = Math.Min(step.ParentNote.StartLaneIndex + beforeStepPos.LaneIndexOffset + step.ParentNote.StartWidth + beforeStepPos.WidthChange - step.ParentNote.StartWidth, Math.Max(-step.ParentNote.StartWidth + 1, widthChange));
                                step.SetPosition(laneIndexOffset, widthChange);
                                Cursor.Current = Cursors.SizeWE;
                            })
                            .Finally(() =>
                            {
                                Cursor.Current = Cursors.Default;
                                var afterPos = new MoveSlideStepNoteOperation.NotePosition(step.TickOffset, step.LaneIndexOffset, step.WidthChange);
                                if (beforeStepPos == afterPos) return;
                                OperationManager.Push(new MoveSlideStepNoteOperation(step, beforeStepPos, afterPos));
                            });
                    }

                    IObservable<MouseEventArgs> rightSlideStepNoteHandler(Slide.StepTap step)
                    {
                        var beforeStepPos = new MoveSlideStepNoteOperation.NotePosition(step.TickOffset, step.LaneIndexOffset, step.WidthChange);

                        return mouseMove
                            .TakeUntil(mouseUp)
                            .Do(q =>
                            {
                                var currentScorePos = GetDrawingMatrix(new Matrix()).GetInvertedMatrix().TransformPoint(q.Location);
                                int xdiff = (int)((currentScorePos.X - scorePos.X) / (UnitLaneWidth + BorderThickness));
                                int widthChange = beforeStepPos.WidthChange + xdiff;
                                step.WidthChange = Math.Min(Constants.LanesCount - step.LaneIndex - step.ParentNote.StartWidth, Math.Max(-step.ParentNote.StartWidth + 1, widthChange));
                                Cursor.Current = Cursors.SizeWE;
                            })
                            .Finally(() =>
                            {
                                Cursor.Current = Cursors.Default;
                                var afterPos = new MoveSlideStepNoteOperation.NotePosition(step.TickOffset, step.LaneIndexOffset, step.WidthChange);
                                if (beforeStepPos == afterPos) return;
                                OperationManager.Push(new MoveSlideStepNoteOperation(step, beforeStepPos, afterPos));
                            });
                    }

                    // 挿入時のハンドラにも流用するのでFinallyつけられない
                    IObservable<MouseEventArgs> moveSlideStepNoteHandler(Slide.StepTap step)
                    {
                        var beforeStepPos = new MoveSlideStepNoteOperation.NotePosition(step.TickOffset, step.LaneIndexOffset, step.WidthChange);
                        var offsets = new HashSet<int>(step.ParentNote.StepNotes.Select(q => q.TickOffset));
                        bool isMaxOffsetStep = step.TickOffset == offsets.Max();
                        offsets.Remove(step.TickOffset);
                        int maxOffset = offsets.OrderByDescending(q => q).FirstOrDefault();
                        return mouseMove
                            .TakeUntil(mouseUp)
                            .Do(q =>
                            {
                                var currentScorePos = GetDrawingMatrix(new Matrix()).GetInvertedMatrix().TransformPoint(q.Location);
                                int offset = GetQuantizedTick(GetTickFromYPosition(currentScorePos.Y)) - step.ParentNote.StartTick;
                                int xdiff = (int)((currentScorePos.X - scorePos.X) / (UnitLaneWidth + BorderThickness));
                                int laneIndexOffset = beforeStepPos.LaneIndexOffset + xdiff;
                                step.LaneIndexOffset = Math.Min(Constants.LanesCount - step.Width - step.ParentNote.StartLaneIndex, Math.Max(-step.ParentNote.StartLaneIndex, laneIndexOffset));
                                // 最終Step以降に移動はさせないし同じTickに置かせもしない
                                if ((!isMaxOffsetStep && offset > maxOffset) || offsets.Contains(offset) || offset <= 0) return;
                                // 最終Stepは手前のStepより前に動かさない……
                                if (isMaxOffsetStep && offset <= maxOffset) return;
                                step.TickOffset = offset;
                                Cursor.Current = Cursors.SizeAll;
                            });
                    }

                    IObservable<MouseEventArgs> slideHandler(Slide slide)
                    {
                        foreach (var step in slide.StepNotes.OrderByDescending(q => q.TickOffset))
                        {
                            RectangleF stepRect = GetClickableRectFromNotePosition(step.Tick, step.LaneIndex, step.Width);
                            var beforeStepPos = new MoveSlideStepNoteOperation.NotePosition(step.TickOffset, step.LaneIndexOffset, step.WidthChange);

                            if (stepRect.Contains(scorePos))
                            {
                                if (stepRect.GetLeftThumb(EdgeHitWidthRate, MinimumEdgeHitWidth).Contains(scorePos))
                                {
                                    return leftSlideStepNoteHandler(step);
                                }

                                if (stepRect.GetRightThumb(EdgeHitWidthRate, MinimumEdgeHitWidth).Contains(scorePos))
                                {
                                    return rightSlideStepNoteHandler(step);
                                }

                                if (stepRect.Contains(scorePos))
                                {
                                    return moveSlideStepNoteHandler(step)
                                        .Finally(() =>
                                        {
                                            Cursor.Current = Cursors.Default;
                                            var afterPos = new MoveSlideStepNoteOperation.NotePosition(step.TickOffset, step.LaneIndexOffset, step.WidthChange);
                                            if (beforeStepPos == afterPos) return;
                                            OperationManager.Push(new MoveSlideStepNoteOperation(step, beforeStepPos, afterPos));
                                        });
                                }
                            }
                        }

                        RectangleF startRect = GetClickableRectFromNotePosition(slide.StartNote.Tick, slide.StartNote.LaneIndex, slide.StartNote.Width);

                        int leftStepLaneIndexOffset = Math.Min(0, slide.StepNotes.Min(q => q.LaneIndexOffset));
                        int rightStepLaneIndexOffset = Math.Max(0, slide.StepNotes.Max(q => q.LaneIndexOffset + q.WidthChange)); // 最も右にあるStepNoteの右端に対するStartNoteの右端からのオフセット
                        int minWidthChange = Math.Min(0, slide.StepNotes.Min(q => q.WidthChange));

                        var beforePos = new MoveSlideOperation.NotePosition(slide.StartTick, slide.StartLaneIndex, slide.StartWidth);
                        if (startRect.GetLeftThumb(EdgeHitWidthRate, MinimumEdgeHitWidth).Contains(scorePos))
                        {
                            return mouseMove
                                .TakeUntil(mouseUp)
                                .Do(q =>
                                {
                                    var currentScorePos = GetDrawingMatrix(new Matrix()).GetInvertedMatrix().TransformPoint(q.Location);
                                    int xdiff = (int)((currentScorePos.X - scorePos.X) / (UnitLaneWidth + BorderThickness));
                                    xdiff = Math.Min(beforePos.StartWidth + minWidthChange - 1, Math.Max(-beforePos.StartLaneIndex - leftStepLaneIndexOffset, xdiff));
                                    int width = beforePos.StartWidth - xdiff;
                                    int laneIndex = beforePos.StartLaneIndex + xdiff;
                                    // clamp
                                    width = Math.Min(Constants.LanesCount - slide.StartLaneIndex - leftStepLaneIndexOffset, Math.Max(-minWidthChange + 1, width));
                                    laneIndex = Math.Min(Constants.LanesCount - rightStepLaneIndexOffset, Math.Max(-leftStepLaneIndexOffset - beforePos.StartLaneIndex, laneIndex));
                                    slide.SetPosition(laneIndex, width);
                                    Cursor.Current = Cursors.SizeWE;
                                })
                                .Finally(() =>
                                {
                                    Cursor.Current = Cursors.Default;
                                    LastWidth = slide.StartWidth;
                                    var afterPos = new MoveSlideOperation.NotePosition(slide.StartTick, slide.StartLaneIndex, slide.StartWidth);
                                    if (beforePos == afterPos) return;
                                    OperationManager.Push(new MoveSlideOperation(slide, beforePos, afterPos));
                                });
                        }

                        if (startRect.GetRightThumb(EdgeHitWidthRate, MinimumEdgeHitWidth).Contains(scorePos))
                        {
                            return mouseMove
                                .TakeUntil(mouseUp)
                                .Do(q =>
                                {
                                    var currentScorePos = GetDrawingMatrix(new Matrix()).GetInvertedMatrix().TransformPoint(q.Location);
                                    int xdiff = (int)((currentScorePos.X - scorePos.X) / (UnitLaneWidth + BorderThickness));
                                    int width = beforePos.StartWidth + xdiff;
                                    slide.StartWidth = Math.Min(Constants.LanesCount - slide.StartLaneIndex - rightStepLaneIndexOffset, Math.Max(-minWidthChange + 1, width));
                                    Cursor.Current = Cursors.SizeWE;
                                })
                                .Finally(() =>
                                {
                                    Cursor.Current = Cursors.Default;
                                    LastWidth = slide.StartWidth;
                                    var afterPos = new MoveSlideOperation.NotePosition(slide.StartTick, slide.StartLaneIndex, slide.StartWidth);
                                    if (beforePos == afterPos) return;
                                    OperationManager.Push(new MoveSlideOperation(slide, beforePos, afterPos));
                                });
                        }

                        if (startRect.Contains(scorePos))
                        {
                            int beforeLaneIndex = slide.StartNote.LaneIndex;
                            return mouseMove
                                .TakeUntil(mouseUp)
                                .Do(q =>
                                {
                                    var currentScorePos = GetDrawingMatrix(new Matrix()).GetInvertedMatrix().TransformPoint(q.Location);
                                    slide.StartTick = Math.Max(GetQuantizedTick(GetTickFromYPosition(currentScorePos.Y)), 0);
                                    int xdiff = (int)((currentScorePos.X - scorePos.X) / (UnitLaneWidth + BorderThickness));
                                    int laneIndex = beforeLaneIndex + xdiff;
                                    slide.StartLaneIndex = Math.Min(Constants.LanesCount - slide.StartWidth - rightStepLaneIndexOffset, Math.Max(-leftStepLaneIndexOffset, laneIndex));
                                    Cursor.Current = Cursors.SizeAll;
                                })
                                .Finally(() =>
                                {
                                    Cursor.Current = Cursors.Default;
                                    LastWidth = slide.StartWidth;
                                    var afterPos = new MoveSlideOperation.NotePosition(slide.StartTick, slide.StartLaneIndex, slide.StartWidth);
                                    if (beforePos == afterPos) return;
                                    OperationManager.Push(new MoveSlideOperation(slide, beforePos, afterPos));
                                });
                        }

                        return null;
                    }

                    IObservable<MouseEventArgs> holdHandler(Hold hold)
                    {
                        // HOLD長さ変更
                        if (GetClickableRectFromNotePosition(hold.EndNote.Tick, hold.LaneIndex, hold.Width).Contains(scorePos))
                        {
                            int beforeDuration = hold.Duration;
                            return holdDurationHandler(hold)
                                .Finally(() =>
                                {
                                    if (beforeDuration == hold.Duration) return;
                                    OperationManager.Push(new ChangeHoldDurationOperation(hold, beforeDuration, hold.Duration));
                                });
                        }

                        RectangleF startRect = GetClickableRectFromNotePosition(hold.StartTick, hold.LaneIndex, hold.Width);

                        var beforePos = new MoveHoldOperation.NotePosition(hold.StartTick, hold.LaneIndex, hold.Width);
                        if (startRect.GetLeftThumb(EdgeHitWidthRate, MinimumEdgeHitWidth).Contains(scorePos))
                        {
                            return mouseMove
                                .TakeUntil(mouseUp)
                                .Do(q =>
                                {
                                    var currentScorePos = GetDrawingMatrix(new Matrix()).GetInvertedMatrix().TransformPoint(q.Location);
                                    int xdiff = (int)((currentScorePos.X - scorePos.X) / (UnitLaneWidth + BorderThickness));
                                    xdiff = Math.Min(beforePos.Width - 1, Math.Max(-beforePos.LaneIndex, xdiff));
                                    int width = beforePos.Width - xdiff;
                                    int laneIndex = beforePos.LaneIndex + xdiff;
                                    width = Math.Min(Constants.LanesCount - laneIndex, Math.Max(1, width));
                                    laneIndex = Math.Min(Constants.LanesCount - width, Math.Max(0, laneIndex));
                                    hold.SetPosition(laneIndex, width);
                                    Cursor.Current = Cursors.SizeWE;
                                })
                                .Finally(() =>
                                {
                                    Cursor.Current = Cursors.Default;
                                    LastWidth = hold.Width;
                                    var afterPos = new MoveHoldOperation.NotePosition(hold.StartTick, hold.LaneIndex, hold.Width);
                                    if (beforePos == afterPos) return;
                                    OperationManager.Push(new MoveHoldOperation(hold, beforePos, afterPos));
                                });
                        }

                        if (startRect.GetRightThumb(EdgeHitWidthRate, MinimumEdgeHitWidth).Contains(scorePos))
                        {
                            return mouseMove
                                .TakeUntil(mouseUp)
                                .Do(q =>
                                {
                                    var currentScorePos = GetDrawingMatrix(new Matrix()).GetInvertedMatrix().TransformPoint(q.Location);
                                    int xdiff = (int)((currentScorePos.X - scorePos.X) / (UnitLaneWidth + BorderThickness));
                                    int width = beforePos.Width + xdiff;
                                    hold.Width = Math.Min(Constants.LanesCount - hold.LaneIndex, Math.Max(1, width));
                                    Cursor.Current = Cursors.SizeWE;
                                })
                                .Finally(() =>
                                {
                                    Cursor.Current = Cursors.Default;
                                    LastWidth = hold.Width;
                                    var afterPos = new MoveHoldOperation.NotePosition(hold.StartTick, hold.LaneIndex, hold.Width);
                                    if (beforePos == afterPos) return;
                                    OperationManager.Push(new MoveHoldOperation(hold, beforePos, afterPos));
                                });
                        }

                        if (startRect.Contains(scorePos))
                        {
                            return mouseMove
                                .TakeUntil(mouseUp)
                                .Do(q =>
                                {
                                    var currentScorePos = GetDrawingMatrix(new Matrix()).GetInvertedMatrix().TransformPoint(q.Location);
                                    hold.StartTick = Math.Max(GetQuantizedTick(GetTickFromYPosition(currentScorePos.Y)), 0);
                                    int xdiff = (int)((currentScorePos.X - scorePos.X) / (UnitLaneWidth + BorderThickness));
                                    int laneIndex = beforePos.LaneIndex + xdiff;
                                    hold.LaneIndex = Math.Min(Constants.LanesCount - hold.Width, Math.Max(0, laneIndex));
                                    Cursor.Current = Cursors.SizeAll;
                                })
                                .Finally(() =>
                                {
                                    Cursor.Current = Cursors.Default;
                                    LastWidth = hold.Width;
                                    var afterPos = new MoveHoldOperation.NotePosition(hold.StartTick, hold.LaneIndex, hold.Width);
                                    if (beforePos == afterPos) return;
                                    OperationManager.Push(new MoveHoldOperation(hold, beforePos, afterPos));
                                });
                        }

                        return null;
                    }

                    IObservable<MouseEventArgs> surfaceNotesHandler()
                    {
                        foreach (var note in Notes.Damages.Reverse().Where(q => q.Tick >= HeadTick && q.Tick <= tailTick))
                        {
                            var subscription = shortNoteHandler(note);
                            if (subscription != null) return subscription;
                        }

                        foreach (var note in Notes.ExTaps.Reverse().Where(q => q.Tick >= HeadTick && q.Tick <= tailTick))
                        {
                            var subscription = shortNoteHandler(note);
                            if (subscription != null) return subscription;
                        }

                        foreach (var note in Notes.Taps.Reverse().Where(q => q.Tick >= HeadTick && q.Tick <= tailTick))
                        {
                            var subscription = shortNoteHandler(note);
                            if (subscription != null) return subscription;
                        }

                        foreach (var note in Notes.Flicks.Reverse().Where(q => q.Tick >= HeadTick && q.Tick <= tailTick))
                        {
                            var subscription = shortNoteHandler(note);
                            if (subscription != null) return subscription;
                        }

                        foreach (var note in Notes.Slides.Reverse().Where(q => q.StartTick <= tailTick && q.StartTick + q.GetDuration() >= HeadTick))
                        {
                            var subscription = slideHandler(note);
                            if (subscription != null) return subscription;
                        }

                        foreach (var note in Notes.Holds.Reverse().Where(q => q.StartTick <= tailTick && q.StartTick + q.GetDuration() >= HeadTick))
                        {
                            var subscription = holdHandler(note);
                            if (subscription != null) return subscription;
                        }

                        return null;
                    }

                    // AIR系編集時
                    if ((NoteType.Air | NoteType.AirAction).HasFlag(NewNoteType))
                    {
                        var airables = Enumerable.Empty<IAirable>()
                            .Concat(Notes.Damages.Reverse())
                            .Concat(Notes.ExTaps.Reverse())
                            .Concat(Notes.Taps.Reverse())
                            .Concat(Notes.Flicks.Reverse())
                            .Concat(Notes.Slides.Reverse().Select(q => q.StepNotes.OrderByDescending(r => r.TickOffset).First()))
                            .Concat(Notes.Holds.Reverse().Select(q => q.EndNote));

                        IObservable<MouseEventArgs> addAirHandler()
                        {
                            foreach (var note in airables)
                            {
                                RectangleF rect = GetClickableRectFromNotePosition(note.Tick, note.LaneIndex, note.Width);
                                if (rect.Contains(scorePos))
                                {
                                    // 既に配置されていれば追加しない
                                    if (Notes.GetReferencedAir(note).Count() > 0) break;
                                    return mouseMove
                                        .TakeUntil(mouseUp)
                                        .Count()
                                        .Zip(mouseUp, (q, r) => new { Args = r, Count = q })
                                        .Where(q => q.Count == 0)
                                        .Select(q => q.Args)
                                        .Do(q =>
                                        {
                                            var air = new Air(note)
                                            {
                                                VerticalDirection = AirDirection.VerticalDirection,
                                                HorizontalDirection = AirDirection.HorizontalDirection
                                            };
                                            Notes.Add(air);
                                            Invalidate();
                                            OperationManager.Push(new InsertAirOperation(Notes, air));
                                        });
                                }
                            }
                            return null;
                        }

                        IObservable<MouseEventArgs> addAirActionHandler()
                        {
                            foreach (var note in Notes.AirActions.Reverse())
                            {
                                var size = new SizeF(UnitLaneWidth / 2, GetYPositionFromTick(note.ActionNotes.Max(q => q.Offset)));
                                var rect = new RectangleF(
                                    (UnitLaneWidth + BorderThickness) * (note.ParentNote.LaneIndex + note.ParentNote.Width / 2f) - size.Width / 2,
                                    GetYPositionFromTick(note.ParentNote.Tick),
                                    size.Width, size.Height);
                                if (rect.Contains(scorePos))
                                {
                                    int offset = GetQuantizedTick(GetTickFromYPosition(scorePos.Y)) - note.ParentNote.Tick;
                                    if (offset > 0 && !note.ActionNotes.Any(q => q.Offset == offset))
                                    {
                                        var action = new AirAction.ActionNote(note) { Offset = offset };
                                        note.ActionNotes.Add(action);
                                        Invalidate();
                                        return actionNoteHandler(action)
                                            .Finally(() => OperationManager.Push(new InsertAirActionNoteOperation(note, action)));
                                    }
                                }
                            }

                            foreach (var note in airables)
                            {
                                RectangleF rect = GetClickableRectFromNotePosition(note.Tick, note.LaneIndex, note.Width);
                                if (rect.Contains(scorePos))
                                {
                                    // 既に配置されていれば追加しない
                                    if (Notes.GetReferencedAirAction(note).Count() > 0) break;
                                    var airAction = new AirAction(note);
                                    var action = new AirAction.ActionNote(airAction) { Offset = (int)QuantizeTick };
                                    airAction.ActionNotes.Add(action);
                                    var op = new InsertAirActionOperation(Notes, airAction);
                                    IOperation comp = InsertAirWithAirAction && Notes.GetReferencedAir(note).Count() == 0 ? (IOperation)new CompositeOperation("AIR, AIR-ACTIONの追加", new IOperation[] { new InsertAirOperation(Notes, new Air(note)), op }) : op;
                                    comp.Redo();
                                    Invalidate();
                                    return actionNoteHandler(action)
                                        .Finally(() => OperationManager.Push(comp));
                                }
                            }

                            return null;
                        }

                        switch (NewNoteType)
                        {
                            case NoteType.Air:
                                // クリック後MouseMoveするならノーツ操作 / MouseUpならAIR追加
                                return Observable.Merge(surfaceNotesHandler() ?? Observable.Empty<MouseEventArgs>(), addAirHandler() ?? Observable.Empty<MouseEventArgs>());

                            case NoteType.AirAction:
                                // AIR-ACTION追加時はその後のドラッグをハンドルする
                                return addAirActionHandler() ?? surfaceNotesHandler() ?? Observable.Empty<MouseEventArgs>();
                        }
                    }
                    else
                    {
                        var subscription = surfaceNotesHandler();
                        if (subscription != null) return subscription;
                    }

                    // なんもねえなら追加だァ！
                    if ((NoteType.Tap | NoteType.ExTap | NoteType.Flick | NoteType.Damage).HasFlag(NewNoteType))
                    {
                        TappableBase newNote = null;
                        IOperation op = null;
                        switch (NewNoteType)
                        {
                            case NoteType.Tap:
                                var tap = new Tap();
                                Notes.Add(tap);
                                newNote = tap;
                                op = new InsertTapOperation(Notes, tap);
                                break;

                            case NoteType.ExTap:
                                var extap = new ExTap();
                                Notes.Add(extap);
                                newNote = extap;
                                op = new InsertExTapOperation(Notes, extap);
                                break;

                            case NoteType.Flick:
                                var flick = new Flick();
                                Notes.Add(flick);
                                newNote = flick;
                                op = new InsertFlickOperation(Notes, flick);
                                break;

                            case NoteType.Damage:
                                var damage = new Damage();
                                Notes.Add(damage);
                                newNote = damage;
                                op = new InsertDamageOperation(Notes, damage);
                                break;
                        }
                        newNote.Width = LastWidth;
                        newNote.Tick = Math.Max(GetQuantizedTick(GetTickFromYPosition(scorePos.Y)), 0);
                        newNote.LaneIndex = GetNewNoteLaneIndex(scorePos.X, newNote.Width);
                        Invalidate();
                        return moveTappableNoteHandler(newNote)
                            .Finally(() => OperationManager.Push(op));
                    }
                    else
                    {
                        switch (NewNoteType)
                        {
                            case NoteType.Hold:
                                var hold = new Hold
                                {
                                    StartTick = Math.Max(GetQuantizedTick(GetTickFromYPosition(scorePos.Y)), 0),
                                    Width = LastWidth,
                                    Duration = (int)QuantizeTick
                                };
                                hold.LaneIndex = GetNewNoteLaneIndex(scorePos.X, hold.Width);
                                Notes.Add(hold);
                                Invalidate();
                                return holdDurationHandler(hold)
                                    .Finally(() => OperationManager.Push(new InsertHoldOperation(Notes, hold)));

                            case NoteType.Slide:
                                // 中継点
                                foreach (var note in Notes.Slides.Reverse())
                                {
                                    var bg = new Slide.TapBase[] { note.StartNote }.Concat(note.StepNotes.OrderBy(q => q.Tick)).ToList();
                                    for (int i = 0; i < bg.Count - 1; i++)
                                    {
                                        // 描画時のコードコピペつらい
                                        var path = NoteGraphics.GetSlideBackgroundPath(
                                            (UnitLaneWidth + BorderThickness) * bg[i].Width - BorderThickness,
                                            (UnitLaneWidth + BorderThickness) * bg[i + 1].Width - BorderThickness,
                                            (UnitLaneWidth + BorderThickness) * bg[i].LaneIndex,
                                            GetYPositionFromTick(bg[i].Tick),
                                            (UnitLaneWidth + BorderThickness) * bg[i + 1].LaneIndex,
                                            GetYPositionFromTick(bg[i + 1].Tick));
                                        if (path.PathPoints.ContainsPoint(scorePos))
                                        {
                                            int tickOffset = GetQuantizedTick(GetTickFromYPosition(scorePos.Y)) - note.StartTick;
                                            // 同一Tickに追加させない
                                            if (tickOffset != 0 && !note.StepNotes.Any(q => q.TickOffset == tickOffset))
                                            {
                                                int width = note.StepNotes.OrderBy(q => q.TickOffset).LastOrDefault(q => q.TickOffset <= tickOffset)?.Width ?? note.StartWidth;
                                                int laneIndex = (int)(scorePos.X / (UnitLaneWidth + BorderThickness)) - width / 2;
                                                laneIndex = Math.Min(Constants.LanesCount - width, Math.Max(0, laneIndex));
                                                var newStep = new Slide.StepTap(note)
                                                {
                                                    TickOffset = tickOffset,
                                                    IsVisible = IsNewSlideStepVisible
                                                };
                                                newStep.SetPosition(laneIndex - note.StartLaneIndex, width - note.StartWidth);
                                                note.StepNotes.Add(newStep);
                                                Invalidate();
                                                return moveSlideStepNoteHandler(newStep)
                                                    .Finally(() => OperationManager.Push(new InsertSlideStepNoteOperation(note, newStep)));
                                            }
                                        }
                                    }
                                }

                                // 新規SLIDE
                                var slide = new Slide()
                                {
                                    StartTick = Math.Max(GetQuantizedTick(GetTickFromYPosition(scorePos.Y)), 0),
                                    StartWidth = LastWidth
                                };
                                slide.StartLaneIndex = GetNewNoteLaneIndex(scorePos.X, slide.StartWidth);
                                var step = new Slide.StepTap(slide) { TickOffset = (int)QuantizeTick };
                                slide.StepNotes.Add(step);
                                Notes.Add(slide);
                                Invalidate();
                                return moveSlideStepNoteHandler(step)
                                    .Finally(() => OperationManager.Push(new InsertSlideOperation(Notes, slide)));
                        }
                    }
                    return Observable.Empty<MouseEventArgs>();
                }).Subscribe(p => Invalidate());

            IObservable<MouseEventArgs> rangeSelection(PointF startPos)
            {
                SelectedRange = new SelectionRange()
                {
                    StartTick = Math.Max(GetQuantizedTick(GetTickFromYPosition(startPos.Y)), 0),
                    Duration = 0,
                    StartLaneIndex = 0,
                    SelectedLanesCount = 0
                };

                return mouseMove.TakeUntil(mouseUp)
                    .Do(q =>
                    {
                        Matrix currentMatrix = GetDrawingMatrix(new Matrix());
                        currentMatrix.Invert();
                        var scorePos = currentMatrix.TransformPoint(q.Location);

                        int startLaneIndex = Math.Min(Math.Max((int)startPos.X / (UnitLaneWidth + BorderThickness), 0), Constants.LanesCount - 1);
                        int endLaneIndex = Math.Min(Math.Max((int)scorePos.X / (UnitLaneWidth + BorderThickness), 0), Constants.LanesCount - 1);
                        int endTick = GetQuantizedTick(GetTickFromYPosition(scorePos.Y));

                        SelectedRange = new SelectionRange()
                        {
                            StartTick = SelectedRange.StartTick,
                            Duration = endTick - SelectedRange.StartTick,
                            StartLaneIndex = Math.Min(startLaneIndex, endLaneIndex),
                            SelectedLanesCount = Math.Abs(endLaneIndex - startLaneIndex) + 1
                        };
                    });
            }

            var eraseSubscription = mouseDown
                .Where(p => Editable)
                .Where(p => p.Button == MouseButtons.Left && EditMode == EditMode.Erase)
                .SelectMany(p =>
                {
                    Matrix startMatrix = GetDrawingMatrix(new Matrix());
                    startMatrix.Invert();
                    PointF startScorePos = startMatrix.TransformPoint(p.Location);
                    return rangeSelection(startScorePos)
                        .Count()
                        .Zip(mouseUp, (q, r) => new { Pos = r.Location, Count = q });
                })
                .Do(p =>
                {
                    if (p.Count > 0) // ドラッグで範囲選択された
                    {
                        RemoveSelectedNotes();
                        SelectedRange = SelectionRange.Empty;
                        return;
                    }

                    Matrix matrix = GetDrawingMatrix(new Matrix());
                    matrix.Invert();
                    PointF scorePos = matrix.TransformPoint(p.Pos);

                    IEnumerable<IOperation> removeReferencedAirs(IAirable airable)
                    {
                        var airs = Notes.GetReferencedAir(airable).ToList().Select(q =>
                        {
                            Notes.Remove(q);
                            return new RemoveAirOperation(Notes, q);
                        }).ToList();
                        var airActions = Notes.GetReferencedAirAction(airable).ToList().Select(q =>
                        {
                            Notes.Remove(q);
                            return new RemoveAirActionOperation(Notes, q);
                        }).ToList();

                        return airs.Cast<IOperation>().Concat(airActions);
                    }

                    foreach (var note in Notes.Airs.Reverse())
                    {
                        RectangleF rect = NoteGraphics.GetAirRect(GetRectFromNotePosition(note.ParentNote.Tick, note.ParentNote.LaneIndex, note.ParentNote.Width));
                        if (rect.Contains(scorePos))
                        {
                            Notes.Remove(note);
                            OperationManager.Push(new RemoveAirOperation(Notes, note));
                            return;
                        }
                    }

                    foreach (var note in Notes.AirActions.Reverse())
                    {
                        foreach (var action in note.ActionNotes)
                        {
                            RectangleF rect = GetClickableRectFromNotePosition(note.StartTick + action.Offset, note.ParentNote.LaneIndex, note.ParentNote.Width);
                            if (rect.Contains(scorePos))
                            {
                                if (note.ActionNotes.Count == 1)
                                {
                                    Notes.Remove(note);
                                    OperationManager.Push(new RemoveAirActionOperation(Notes, note));
                                }
                                else
                                {
                                    note.ActionNotes.Remove(action);
                                    OperationManager.Push(new RemoveAirActionNoteOperation(note, action));
                                }
                                return;
                            }
                        }
                    }

                    foreach (var note in Notes.Damages.Reverse())
                    {
                        RectangleF rect = GetClickableRectFromNotePosition(note.Tick, note.LaneIndex, note.Width);
                        if (rect.Contains(scorePos))
                        {
                            var airOp = removeReferencedAirs(note).ToList();
                            var op = new RemoveDamageOperation(Notes, note);
                            Notes.Remove(note);
                            if (airOp.Count > 0)
                            {
                                OperationManager.Push(new CompositeOperation(op.Description, new IOperation[] { op }.Concat(airOp)));
                            }
                            else
                            {
                                OperationManager.Push(op);
                            }
                            return;
                        }
                    }

                    foreach (var note in Notes.ExTaps.Reverse())
                    {
                        RectangleF rect = GetClickableRectFromNotePosition(note.Tick, note.LaneIndex, note.Width);
                        if (rect.Contains(scorePos))
                        {
                            var airOp = removeReferencedAirs(note).ToList();
                            var op = new RemoveExTapOperation(Notes, note);
                            Notes.Remove(note);
                            if (airOp.Count > 0)
                            {
                                OperationManager.Push(new CompositeOperation(op.Description, new IOperation[] { op }.Concat(airOp)));
                            }
                            else
                            {
                                OperationManager.Push(op);
                            }
                            return;
                        }
                    }

                    foreach (var note in Notes.Taps.Reverse())
                    {
                        RectangleF rect = GetClickableRectFromNotePosition(note.Tick, note.LaneIndex, note.Width);
                        if (rect.Contains(scorePos))
                        {
                            var airOp = removeReferencedAirs(note).ToList();
                            var op = new RemoveTapOperation(Notes, note);
                            Notes.Remove(note);
                            if (airOp.Count > 0)
                            {
                                OperationManager.Push(new CompositeOperation(op.Description, new IOperation[] { op }.Concat(airOp)));
                            }
                            else
                            {
                                OperationManager.Push(op);
                            }
                            return;
                        }
                    }

                    foreach (var note in Notes.Flicks.Reverse())
                    {
                        RectangleF rect = GetClickableRectFromNotePosition(note.Tick, note.LaneIndex, note.Width);
                        if (rect.Contains(scorePos))
                        {
                            var airOp = removeReferencedAirs(note).ToList();
                            var op = new RemoveFlickOperation(Notes, note);
                            Notes.Remove(note);
                            if (airOp.Count > 0)
                            {
                                OperationManager.Push(new CompositeOperation(op.Description, new IOperation[] { op }.Concat(airOp)));
                            }
                            else
                            {
                                OperationManager.Push(op);
                            }
                            return;
                        }
                    }

                    foreach (var slide in Notes.Slides.Reverse())
                    {
                        foreach (var step in slide.StepNotes.OrderBy(q => q.TickOffset).Take(slide.StepNotes.Count - 1))
                        {
                            RectangleF rect = GetClickableRectFromNotePosition(step.Tick, step.LaneIndex, step.Width);
                            if (rect.Contains(scorePos))
                            {
                                slide.StepNotes.Remove(step);
                                OperationManager.Push(new RemoveSlideStepNoteOperation(slide, step));
                                return;
                            }
                        }

                        RectangleF startRect = GetClickableRectFromNotePosition(slide.StartTick, slide.StartLaneIndex, slide.StartWidth);
                        if (startRect.Contains(scorePos))
                        {
                            var airOp = slide.StepNotes.SelectMany(q => removeReferencedAirs(q)).ToList();
                            var op = new RemoveSlideOperation(Notes, slide);
                            Notes.Remove(slide);
                            if (airOp.Count > 0)
                            {
                                OperationManager.Push(new CompositeOperation(op.Description, new IOperation[] { op }.Concat(airOp)));
                            }
                            else
                            {
                                OperationManager.Push(op);
                            }
                            return;
                        }
                    }

                    foreach (var hold in Notes.Holds.Reverse())
                    {
                        RectangleF rect = GetClickableRectFromNotePosition(hold.StartTick, hold.LaneIndex, hold.Width);
                        if (rect.Contains(scorePos))
                        {
                            var airOp = removeReferencedAirs(hold.EndNote).ToList();
                            var op = new RemoveHoldOperation(Notes, hold);
                            Notes.Remove(hold);
                            if (airOp.Count > 0)
                            {
                                OperationManager.Push(new CompositeOperation(op.Description, new IOperation[] { op }.Concat(airOp)));
                            }
                            else
                            {
                                OperationManager.Push(op);
                            }
                            return;
                        }
                    }
                })
                .Subscribe(p => Invalidate());

            var selectSubscription = mouseDown
                .Where(p => Editable)
                .Where(p => p.Button == MouseButtons.Left && EditMode == EditMode.Select)
                .SelectMany(p =>
                {
                    Matrix startMatrix = GetDrawingMatrix(new Matrix());
                    startMatrix.Invert();
                    PointF startScorePos = startMatrix.TransformPoint(p.Location);

                    if (GetSelectionRect().Contains(Point.Ceiling(startScorePos)))
                    {
                        int minTick = SelectedRange.StartTick + (SelectedRange.Duration < 0 ? SelectedRange.Duration : 0);
                        int maxTick = SelectedRange.StartTick + (SelectedRange.Duration < 0 ? 0 : SelectedRange.Duration);
                        int startTick = SelectedRange.StartTick;
                        int startLaneIndex = SelectedRange.StartLaneIndex;
                        int endLaneIndex = SelectedRange.StartLaneIndex + SelectedRange.SelectedLanesCount;

                        var selectedNotes = GetSelectedNotes();
                        var dicShortNotes = selectedNotes.GetShortNotes().ToDictionary(q => q, q => new MoveShortNoteOperation.NotePosition(q.Tick, q.LaneIndex));
                        var dicHolds = selectedNotes.Holds.ToDictionary(q => q, q => new MoveHoldOperation.NotePosition(q.StartTick, q.LaneIndex, q.Width));
                        var dicSlides = selectedNotes.Slides.ToDictionary(q => q, q => new MoveSlideOperation.NotePosition(q.StartTick, q.StartLaneIndex, q.StartWidth));

                        // 選択範囲移動
                        return mouseMove.TakeUntil(mouseUp).Do(q =>
                        {
                            Matrix currentMatrix = GetDrawingMatrix(new Matrix());
                            currentMatrix.Invert();
                            var scorePos = currentMatrix.TransformPoint(q.Location);

                            int xdiff = (int)((scorePos.X - startScorePos.X) / (UnitLaneWidth + BorderThickness));
                            int laneIndex = startLaneIndex + xdiff;

                            SelectedRange = new SelectionRange()
                            {
                                StartTick = startTick + Math.Max(GetQuantizedTick(GetTickFromYPosition(scorePos.Y) - GetTickFromYPosition(startScorePos.Y)), -startTick - (SelectedRange.Duration < 0 ? SelectedRange.Duration : 0)),
                                Duration = SelectedRange.Duration,
                                StartLaneIndex = Math.Min(Math.Max(laneIndex, 0), Constants.LanesCount - SelectedRange.SelectedLanesCount),
                                SelectedLanesCount = SelectedRange.SelectedLanesCount
                            };

                            foreach (var item in dicShortNotes)
                            {
                                item.Key.Tick = item.Value.Tick + (SelectedRange.StartTick - startTick);
                                item.Key.LaneIndex = item.Value.LaneIndex + (SelectedRange.StartLaneIndex - startLaneIndex);
                            }

                            // ロングノーツは全体が範囲内に含まれているもののみを対象にするので範囲外移動は考えてない
                            foreach (var item in dicHolds)
                            {
                                item.Key.StartTick = item.Value.StartTick + (SelectedRange.StartTick - startTick);
                                item.Key.LaneIndex = item.Value.LaneIndex + (SelectedRange.StartLaneIndex - startLaneIndex);
                            }

                            foreach (var item in dicSlides)
                            {
                                item.Key.StartTick = item.Value.StartTick + (SelectedRange.StartTick - startTick);
                                item.Key.StartLaneIndex = item.Value.StartLaneIndex + (SelectedRange.StartLaneIndex - startLaneIndex);
                            }

                            // AIR-ACTIONはOffsetの管理面倒で実装できませんでした。許せ

                            Invalidate();
                        })
                        .Finally(() =>
                        {
                            var opShortNotes = dicShortNotes.Select(q =>
                            {
                                var after = new MoveShortNoteOperation.NotePosition(q.Key.Tick, q.Key.LaneIndex);
                                return new MoveShortNoteOperation(q.Key, q.Value, after);
                            });

                            var opHolds = dicHolds.Select(q =>
                            {
                                var after = new MoveHoldOperation.NotePosition(q.Key.StartTick, q.Key.LaneIndex, q.Key.Width);
                                return new MoveHoldOperation(q.Key, q.Value, after);
                            });

                            var opSlides = dicSlides.Select(q =>
                            {
                                var after = new MoveSlideOperation.NotePosition(q.Key.StartTick, q.Key.StartLaneIndex, q.Key.StartWidth);
                                return new MoveSlideOperation(q.Key, q.Value, after);
                            });

                            // 同じ位置に戻ってきたら操作扱いにしない
                            if (startTick == SelectedRange.StartTick && startLaneIndex == SelectedRange.StartLaneIndex) return;
                            OperationManager.Push(new CompositeOperation("ノーツの移動", opShortNotes.Cast<IOperation>().Concat(opHolds).Concat(opSlides).ToList()));
                        });
                    }
                    else
                    {
                        // 範囲選択
                        CurrentTick = Math.Max(GetQuantizedTick(GetTickFromYPosition(startScorePos.Y)), 0);
                        return rangeSelection(startScorePos);
                    }
                }).Subscribe();

            Subscriptions.Add(mouseMoveSubscription);
            Subscriptions.Add(dragSubscription);
            Subscriptions.Add(editSubscription);
            Subscriptions.Add(eraseSubscription);
            Subscriptions.Add(selectSubscription);
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);
            Matrix matrix = GetDrawingMatrix(new Matrix());
            matrix.Invert();

            if (EditMode == EditMode.Select && Editable)
            {
                var scorePos = matrix.TransformPoint(e.Location);
                Cursor = GetSelectionRect().Contains(scorePos) ? Cursors.SizeAll : Cursors.Default;
            }
        }

        protected override void OnMouseDoubleClick(MouseEventArgs e)
        {
            base.OnMouseDoubleClick(e);

            if (e.Button == MouseButtons.Right)
            {
                EditMode = EditMode == EditMode.Edit ? EditMode.Select : EditMode.Edit;
            }
        }

        protected override void OnPaint(PaintEventArgs pe)
        {
            base.OnPaint(pe);

            // Y軸の正方向をTick増加方向として描画 (y = 0 はコントロール下端)
            // コントロールの中心に描画したいなら後でTranslateしといてね
            var prevMatrix = pe.Graphics.Transform;
            pe.Graphics.Transform = GetDrawingMatrix(prevMatrix);

            var dc = new DrawingContext(pe.Graphics, ColorProfile);

            float laneWidth = LaneWidth;
            int tailTick = HeadTick + (int)(ClientSize.Height * UnitBeatTick / UnitBeatHeight);

            // レーン分割線描画
            using (var lightPen = new Pen(LaneBorderLightColor, BorderThickness))
            using (var darkPen = new Pen(LaneBorderDarkColor, BorderThickness))
            {
                for (int i = 0; i <= Constants.LanesCount; i++)
                {
                    float x = i * (UnitLaneWidth + BorderThickness);
                    pe.Graphics.DrawLine(i % 2 == 0 ? lightPen : darkPen, x, GetYPositionFromTick(HeadTick), x, GetYPositionFromTick(tailTick));
                }
            }


            // 時間ガイドの描画
            // そのイベントが含まれる小節(ただし[小節開始Tick, 小節開始Tick + 小節Tick)の範囲)からその拍子を適用
            var sigs = ScoreEvents.TimeSignatureChangeEvents.OrderBy(p => p.Tick).ToList();

            using (var beatPen = new Pen(BeatLineColor, BorderThickness))
            using (var barPen = new Pen(BarLineColor, BorderThickness))
            {
                // 最初の拍子
                int firstBarLength = UnitBeatTick * 4 * sigs[0].Numerator / sigs[0].Denominator;
                int barTick = UnitBeatTick * 4;

                for (int i = HeadTick / (barTick / sigs[0].Denominator); sigs.Count < 2 || i * barTick / sigs[0].Denominator < sigs[1].Tick / firstBarLength * firstBarLength; i++)
                {
                    int tick = i * barTick / sigs[0].Denominator;
                    float y = GetYPositionFromTick(tick);
                    pe.Graphics.DrawLine(i % sigs[0].Numerator == 0 ? barPen : beatPen, 0, y, laneWidth, y);
                    if (tick > tailTick) break;
                }

                // その後の拍子
                int pos = 0;
                for (int j = 1; j < sigs.Count; j++)
                {
                    int prevBarLength = barTick * sigs[j - 1].Numerator / sigs[j - 1].Denominator;
                    int currentBarLength = barTick * sigs[j].Numerator / sigs[j].Denominator;
                    pos += (sigs[j].Tick - pos) / prevBarLength * prevBarLength;
                    if (pos > tailTick) break;
                    for (int i = HeadTick - pos < 0 ? 0 : (HeadTick - pos) / (barTick / sigs[j].Denominator); pos + i * (barTick / sigs[j].Denominator) < tailTick; i++)
                    {
                        if (j < sigs.Count - 1 && i * barTick / sigs[j].Denominator >= (sigs[j + 1].Tick - pos) / currentBarLength * currentBarLength) break;
                        float y = GetYPositionFromTick(pos + i * barTick / sigs[j].Denominator);
                        pe.Graphics.DrawLine(i % sigs[j].Numerator == 0 ? barPen : beatPen, 0, y, laneWidth, y);
                    }
                }
            }

            using (var posPen = new Pen(Color.FromArgb(196, 0, 0)))
            {
                float y = GetYPositionFromTick(CurrentTick);
                pe.Graphics.DrawLine(posPen, -UnitLaneWidth * 2, y, laneWidth, y);
            }

            // ノート描画
            var holds = Notes.Holds.Where(p => p.StartTick <= tailTick && p.StartTick + p.GetDuration() >= HeadTick).ToList();
            // ロングノーツ背景
            // HOLD
            foreach (var hold in holds)
            {
                dc.DrawHoldBackground(new RectangleF(
                    (UnitLaneWidth + BorderThickness) * hold.LaneIndex + BorderThickness,
                    GetYPositionFromTick(hold.StartTick),
                    (UnitLaneWidth + BorderThickness) * hold.Width - BorderThickness,
                    GetYPositionFromTick(hold.Duration)
                    ));
            }

            // SLIDE
            var slides = Notes.Slides.Where(p => p.StartTick <= tailTick && p.StartTick + p.GetDuration() >= HeadTick).ToList();
            foreach (var slide in slides)
            {
                var bg = new Slide.TapBase[] { slide.StartNote }.Concat(slide.StepNotes.OrderBy(p => p.Tick)).ToList();
                var visibleSteps = new Slide.TapBase[] { slide.StartNote }.Concat(slide.StepNotes.Where(p => p.IsVisible).OrderBy(p => p.Tick)).ToList();

                int stepHead = bg.LastOrDefault(p => p.Tick <= HeadTick)?.Tick ?? bg[0].Tick;
                int stepTail = bg.FirstOrDefault(p => p.Tick >= tailTick)?.Tick ?? bg[bg.Count - 1].Tick;
                int visibleHead = visibleSteps.LastOrDefault(p => p.Tick <= HeadTick)?.Tick ?? visibleSteps[0].Tick;
                int visibleTail = visibleSteps.FirstOrDefault(p => p.Tick >= tailTick)?.Tick ?? visibleSteps[visibleSteps.Count - 1].Tick;

                var steps = bg
                    .Where(p => p.Tick >= stepHead && p.Tick <= stepTail)
                    .Select(p => new SlideStepElement()
                    {
                        Point = new PointF((UnitLaneWidth + BorderThickness) * p.LaneIndex, GetYPositionFromTick(p.Tick)),
                        Width = (UnitLaneWidth + BorderThickness) * p.Width - BorderThickness
                    });
                var visibleStepPos = visibleSteps
                    .Where(p => p.Tick >= visibleHead && p.Tick <= visibleTail)
                    .Select(p => GetYPositionFromTick(p.Tick));

                if (stepHead == stepTail) continue;
                dc.DrawSlideBackground(steps, visibleStepPos, ShortNoteHeight);
            }

            var airs = Notes.Airs.Where(p => p.Tick >= HeadTick && p.Tick <= tailTick).ToList();
            var airActions = Notes.AirActions.Where(p => p.StartTick <= tailTick && p.StartTick + p.GetDuration() >= HeadTick).ToList();

            // AIR-ACTION(ガイド線)
            foreach (var note in airActions)
            {
                dc.DrawAirHoldLine(
                    (UnitLaneWidth + BorderThickness) * (note.ParentNote.LaneIndex + note.ParentNote.Width / 2f),
                    GetYPositionFromTick(note.StartTick),
                    GetYPositionFromTick(note.StartTick + note.GetDuration()),
                    ShortNoteHeight);
            }

            // ロングノーツ終点AIR
            foreach (var note in airs)
            {
                if (!(note.ParentNote is LongNoteTapBase)) continue;
                RectangleF rect = GetRectFromNotePosition(note.ParentNote.Tick, note.ParentNote.LaneIndex, note.ParentNote.Width);
                dc.DrawAirStep(rect);
            }

            // 中継点
            foreach (var hold in holds)
            {
                if (Notes.GetReferencedAir(hold.EndNote).Count() > 0) continue; // AIR付き終点
                dc.DrawHoldEnd(GetRectFromNotePosition(hold.StartTick + hold.Duration, hold.LaneIndex, hold.Width));
            }

            foreach (var slide in slides)
            {
                foreach (var step in slide.StepNotes.OrderBy(p => p.TickOffset))
                {
                    if (!Editable && !step.IsVisible) continue;
                    if (Notes.GetReferencedAir(step).Count() > 0) break; // AIR付き終点
                    RectangleF rect = GetRectFromNotePosition(step.Tick, step.LaneIndex, step.Width);
                    if (step.IsVisible) dc.DrawSlideStep(rect);
                    else dc.DrawBorder(rect);
                }
            }

            // 始点
            foreach (var hold in holds)
            {
                dc.DrawHoldBegin(GetRectFromNotePosition(hold.StartTick, hold.LaneIndex, hold.Width));
            }

            foreach (var slide in slides)
            {
                dc.DrawSlideBegin(GetRectFromNotePosition(slide.StartTick, slide.StartNote.LaneIndex, slide.StartWidth));
            }

            // TAP, ExTAP, FLICK, DAMAGE
            foreach (var note in Notes.Flicks.Where(p => p.Tick >= HeadTick && p.Tick <= tailTick))
            {
                dc.DrawFlick(GetRectFromNotePosition(note.Tick, note.LaneIndex, note.Width));
            }

            foreach (var note in Notes.Taps.Where(p => p.Tick >= HeadTick && p.Tick <= tailTick))
            {
                dc.DrawTap(GetRectFromNotePosition(note.Tick, note.LaneIndex, note.Width));
            }

            foreach (var note in Notes.ExTaps.Where(p => p.Tick >= HeadTick && p.Tick <= tailTick))
            {
                dc.DrawExTap(GetRectFromNotePosition(note.Tick, note.LaneIndex, note.Width));
            }

            foreach (var note in Notes.Damages.Where(p => p.Tick >= HeadTick && p.Tick <= tailTick))
            {
                dc.DrawDamage(GetRectFromNotePosition(note.Tick, note.LaneIndex, note.Width));
            }

            // AIR-ACTION(ActionNote)
            foreach (var action in airActions)
            {
                foreach (var note in action.ActionNotes)
                {
                    dc.DrawAirAction(GetRectFromNotePosition(action.StartTick + note.Offset, action.ParentNote.LaneIndex, action.ParentNote.Width).Expand(-ShortNoteHeight * 0.28f));
                }
            }

            // AIR
            foreach (var note in airs)
            {
                RectangleF rect = GetRectFromNotePosition(note.ParentNote.Tick, note.ParentNote.LaneIndex, note.ParentNote.Width);
                dc.DrawAir(rect, note.VerticalDirection, note.HorizontalDirection);
            }

            // 選択範囲描画
            if (Editable) DrawSelectionRange(pe.Graphics);

            // Y軸反転させずにTick = 0をY軸原点とする座標系へ
            pe.Graphics.Transform = GetDrawingMatrix(prevMatrix, false);

            using (var font = new Font("MS Gothic", 8))
            {
                SizeF strSize = pe.Graphics.MeasureString("000", font);

                // 小節番号描画
                int barTick = UnitBeatTick * 4;
                int barCount = 0;
                int pos = 0;

                for (int j = 0; j < sigs.Count; j++)
                {
                    if (pos > tailTick) break;
                    int currentBarLength = (UnitBeatTick * 4) * sigs[j].Numerator / sigs[j].Denominator;
                    for (int i = 0; pos + i * currentBarLength < tailTick; i++)
                    {
                        if (j < sigs.Count - 1 && i * currentBarLength >= (sigs[j + 1].Tick - pos) / currentBarLength * currentBarLength) break;

                        int tick = pos + i * currentBarLength;
                        barCount++;
                        if (tick < HeadTick) continue;
                        var point = new PointF(-strSize.Width, -GetYPositionFromTick(tick) - strSize.Height);
                        pe.Graphics.DrawString(string.Format("{0:000}", barCount), font, Brushes.White, point);
                    }

                    if (j < sigs.Count - 1)
                        pos += (sigs[j + 1].Tick - pos) / currentBarLength * currentBarLength;
                }

                float rightBase = (UnitLaneWidth + BorderThickness) * Constants.LanesCount + strSize.Width / 3;

                // BPM描画
                using (var bpmBrush = new SolidBrush(Color.FromArgb(0, 192, 0)))
                {
                    foreach (var item in ScoreEvents.BpmChangeEvents.Where(p => p.Tick >= HeadTick && p.Tick < tailTick))
                    {
                        var point = new PointF(rightBase, -GetYPositionFromTick(item.Tick) - strSize.Height);
                        pe.Graphics.DrawString(Regex.Replace(item.Bpm.ToString(), @"\.0$", "").PadLeft(3), font, Brushes.Lime, point);
                    }
                }

                // 拍子記号描画
                using (var sigBrush = new SolidBrush(Color.FromArgb(216, 116, 0)))
                {
                    foreach (var item in sigs.Where(p => p.Tick >= HeadTick && p.Tick < tailTick))
                    {
                        var point = new PointF(rightBase + strSize.Width, -GetYPositionFromTick(item.Tick) - strSize.Height);
                        pe.Graphics.DrawString(string.Format("{0}/{1}", item.Numerator, item.Denominator), font, sigBrush, point);
                    }
                }

                // ハイスピ描画
                using (var highSpeedBrush = new SolidBrush(Color.FromArgb(216, 0, 64)))
                {
                    foreach (var item in ScoreEvents.HighSpeedChangeEvents.Where(p => p.Tick >= HeadTick && p.Tick < tailTick))
                    {
                        var point = new PointF(rightBase + strSize.Width * 2, -GetYPositionFromTick(item.Tick) - strSize.Height);
                        pe.Graphics.DrawString(string.Format("x{0: 0.00;-0.00}", item.SpeedRatio), font, highSpeedBrush, point);
                    }
                }
            }

            pe.Graphics.Transform = prevMatrix;
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
            return GetRectFromNotePosition(tick, laneIndex, width).Expand(1, 3);
        }

        private int GetNewNoteLaneIndex(float xpos, int width)
        {
            int newNoteLaneIndex = (int)Math.Round(xpos / (UnitLaneWidth + BorderThickness) - width / 2);
            return Math.Min(Constants.LanesCount - width, Math.Max(0, newNoteLaneIndex));
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

        public void RemoveSelectedEvents()
        {
            int minTick = SelectedRange.StartTick + (SelectedRange.Duration < 0 ? SelectedRange.Duration : 0);
            int maxTick = SelectedRange.StartTick + (SelectedRange.Duration < 0 ? 0 : SelectedRange.Duration);
            bool isContained(EventBase p) => p.Tick != 0 && minTick <= p.Tick && maxTick >= p.Tick;
            var events = ScoreEvents;

            var bpmOp = events.BpmChangeEvents.Where(p => isContained(p)).ToList().Select(p =>
            {
                return new RemoveEventOperation<BpmChangeEvent>(events.BpmChangeEvents, p);
            });

            var speedOp = events.HighSpeedChangeEvents.Where(p => isContained(p)).ToList().Select(p =>
            {
                return new RemoveEventOperation<HighSpeedChangeEvent>(events.HighSpeedChangeEvents, p);
            });

            var signatureOp = events.TimeSignatureChangeEvents.Where(p => isContained(p)).ToList().Select(p =>
            {
                return new RemoveEventOperation<TimeSignatureChangeEvent>(events.TimeSignatureChangeEvents, p);
            });

            OperationManager.InvokeAndPush(new CompositeOperation("イベント削除", bpmOp.Cast<IOperation>().Concat(speedOp).Concat(signatureOp).ToList()));
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


        public void Initialize()
        {
            SelectedRange = SelectionRange.Empty;
            CurrentTick = SelectedRange.StartTick;
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

            private Core.NoteCollection source = new Core.NoteCollection();

            private Dictionary<IAirable, HashSet<Air>> AirDictionary { get; } = new Dictionary<IAirable, HashSet<Air>>();
            private Dictionary<IAirable, HashSet<AirAction>> AirActionDictionary { get; } = new Dictionary<IAirable, HashSet<AirAction>>();

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
                Load(src);
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
                if (!AirDictionary.ContainsKey(note.ParentNote))
                    AirDictionary.Add(note.ParentNote, new HashSet<Air>());
                AirDictionary[note.ParentNote].Add(note);
                NoteChanged?.Invoke(this, EventArgs.Empty);
            }

            public void Add(AirAction note)
            {
                source.AirActions.Add(note);
                if (!AirActionDictionary.ContainsKey(note.ParentNote))
                    AirActionDictionary.Add(note.ParentNote, new HashSet<AirAction>());
                AirActionDictionary[note.ParentNote].Add(note);
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
                AirDictionary[note.ParentNote].Remove(note);
                NoteChanged?.Invoke(this, EventArgs.Empty);
            }

            public void Remove(AirAction note)
            {
                source.AirActions.Remove(note);
                AirActionDictionary[note.ParentNote].Remove(note);
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
                if (!AirDictionary.ContainsKey(note)) return Enumerable.Empty<Air>();
                return AirDictionary[note];
            }

            public IEnumerable<AirAction> GetReferencedAirAction(IAirable note)
            {
                if (!AirActionDictionary.ContainsKey(note)) return Enumerable.Empty<AirAction>();
                return AirActionDictionary[note];
            }

            public int GetLastTick()
            {
                var shortNotes = Taps.Cast<TappableBase>().Concat(ExTaps).Concat(Flicks).Concat(Damages).ToList();
                var longNotes = Holds.Cast<ILongNote>().Concat(Slides).Concat(AirActions).ToList();
                int lastShortNoteTick = shortNotes.Count == 0 ? 0 : shortNotes.Max(p => p.Tick);
                int lastLongNoteTick = longNotes.Count == 0 ? 0 : longNotes.Max(p => p.StartTick + p.GetDuration());
                return Math.Max(lastShortNoteTick, lastLongNoteTick);
            }


            public void Load(Core.NoteCollection collection)
            {
                Clear();

                foreach (var note in collection.Taps) Add(note);
                foreach (var note in collection.ExTaps) Add(note);
                foreach (var note in collection.Holds) Add(note);
                foreach (var note in collection.Slides) Add(note);
                foreach (var note in collection.Airs) Add(note);
                foreach (var note in collection.AirActions) Add(note);
                foreach (var note in collection.Flicks) Add(note);
                foreach (var note in collection.Damages) Add(note);
            }

            public void Clear()
            {
                source = new Core.NoteCollection();

                AirDictionary.Clear();
                AirActionDictionary.Clear();

                NoteChanged?.Invoke(this, EventArgs.Empty);
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

    [Serializable]
    public class SelectionData
    {
        private string serializedText = null;

        [NonSerialized]
        private InnerData Data;

        public int StartTick
        {
            get
            {
                CheckRestored();
                return Data.StartTick;
            }
        }

        public Core.NoteCollection SelectedNotes
        {
            get
            {
                CheckRestored();
                return Data.SelectedNotes;
            }
        }

        public bool IsEmpty
        {
            get
            {
                CheckRestored();
                return SelectedNotes.GetShortNotes().Count() == 0 && SelectedNotes.Holds.Count == 0 && SelectedNotes.Slides.Count == 0 && SelectedNotes.Airs.Count == 0 && SelectedNotes.AirActions.Count == 0;
            }
        }

        public int TicksPerBeat
        {
            get
            {
                CheckRestored();
                return Data.TicksPerBeat;
            }
        }

        public SelectionData()
        {
        }

        public SelectionData(int startTick, int ticksPerBeat, NoteCollection notes)
        {
            Data = new InnerData(startTick, ticksPerBeat, notes);
            serializedText = Newtonsoft.Json.JsonConvert.SerializeObject(Data, SerializerSettings);
        }

        protected void CheckRestored()
        {
            if (Data == null) Restore();
        }

        protected void Restore()
        {
            Data = Newtonsoft.Json.JsonConvert.DeserializeObject<InnerData>(serializedText, SerializerSettings);
        }

        protected static Newtonsoft.Json.JsonSerializerSettings SerializerSettings = new Newtonsoft.Json.JsonSerializerSettings()
        {
            ReferenceLoopHandling = Newtonsoft.Json.ReferenceLoopHandling.Serialize,
            PreserveReferencesHandling = Newtonsoft.Json.PreserveReferencesHandling.Objects,
            TypeNameHandling = Newtonsoft.Json.TypeNameHandling.Auto,
            ContractResolver = new Newtonsoft.Json.Serialization.DefaultContractResolver() { IgnoreSerializableAttribute = true }
        };

        [Newtonsoft.Json.JsonObject(Newtonsoft.Json.MemberSerialization.OptIn)]
        protected class InnerData
        {
            [Newtonsoft.Json.JsonProperty]
            private int startTick;

            [Newtonsoft.Json.JsonProperty]
            private int ticksPerBeat;

            [Newtonsoft.Json.JsonProperty]
            private NoteCollection selectedNotes;

            public int StartTick => startTick;
            public int TicksPerBeat => ticksPerBeat;
            public NoteCollection SelectedNotes => selectedNotes;

            public InnerData(int startTick, int ticksPerBeat, NoteCollection notes)
            {
                this.startTick = startTick;
                this.ticksPerBeat = ticksPerBeat;
                selectedNotes = notes;
            }
        }
    }

    internal static class UIExtensions
    {
        public static Core.NoteCollection Reposit(this NoteView.NoteCollection collection)
        {
            var res = new NoteCollection();
            res.Taps = collection.Taps.ToList();
            res.ExTaps = collection.ExTaps.ToList();
            res.Holds = collection.Holds.ToList();
            res.Slides = collection.Slides.ToList();
            res.Airs = collection.Airs.ToList();
            res.AirActions = collection.AirActions.ToList();
            res.Flicks = collection.Flicks.ToList();
            res.Damages = collection.Damages.ToList();
            return res;
        }
    }
}
