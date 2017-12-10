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

using Ched.Components;
using Ched.UI.Operations;

namespace Ched.UI
{
    public partial class NoteView : Control
    {
        public event EventHandler NewNoteTypeChanged;
        public event EventHandler AirDirectionChanged;

        private NoteType newNoteType = NoteType.Tap;
        private AirDirection airDirection = new AirDirection(VerticalAirDirection.Up, HorizontalAirDirection.Center);

        /// <summary>
        /// 小節の区切り線の色を設定します。
        /// </summary>
        public Color BarLineColor { get; set; } = Color.FromArgb(160, 160, 160);

        /// <summary>
        /// 1拍のガイド線の色を設定します。
        /// </summary>
        public Color BeatLineColor { get; set; } = Color.FromArgb(80, 80, 80);

        /// <summary>
        /// レーンのガイド線の色を設定します。
        /// </summary>
        public Color LaneBorderColor { get; set; } = Color.FromArgb(60, 60, 60);

        /// <summary>
        /// 1レーンあたりの表示幅を設定します。
        /// </summary>
        public int UnitLaneWidth { get; set; } = 12;

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
        public int BorderThickness { get { return (int)Math.Round(UnitLaneWidth * 0.1f); } }

        /// <summary>
        /// ショートノーツの表示高さを設定します。
        /// </summary>
        public int ShortNoteHeight { get; set; } = 5;

        /// <summary>
        /// 1拍あたりのTick数を取得します。
        /// </summary>
        public int UnitBeatTick { get { return 480; } }

        /// <summary>
        /// 1拍あたりの表示高さを設定します。
        /// </summary>
        public float UnitBeatHeight { get; set; } = 80;

        /// <summary>
        /// クォンタイズを行うTick数を指定します。
        /// </summary>
        public int QuantizeTick { get; set; }

        /// <summary>
        /// 表示始端のTickを設定します。
        /// </summary>
        public int HeadTick { get; set; }

        /// <summary>
        /// 表示終端のTickを取得します。
        /// </summary>
        public int TailTick
        {
            get { return HeadTick + (int)(ClientSize.Height * UnitBeatTick / UnitBeatHeight); }
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

        public NoteCollection Notes { get; } = new NoteCollection();

        internal OperationManager OperationManager { get; } = new OperationManager();

        public NoteView()
        {
            InitializeComponent();
            this.DoubleBuffered = true;
            this.BackColor = Color.Black;
            this.SetStyle(ControlStyles.ResizeRedraw, true);
            this.SetStyle(ControlStyles.Opaque, true);

            QuantizeTick = UnitBeatTick;

            var taps = new Tap[]
            {
                new Tap() { Tick = 240, LaneIndex = 0, Width = 4 },
                new ExTap() { Tick = 240, LaneIndex = 4, Width = 3 },
                new Tap() { Tick = 240, LaneIndex = 7, Width = 4 },
                new Tap() { Tick = 480, LaneIndex = 0, Width = 4 },
                new ExTap() { Tick = 480, LaneIndex = 4, Width = 3 },
                new Tap() { Tick = 480, LaneIndex = 7, Width = 4 }
            };
            foreach (var note in taps)
            {
                Notes.Add(note);
            }
            foreach (var note in taps.Skip(3))
                Notes.Add(new Air(note) { HorizontalDirection = HorizontalAirDirection.Right, VerticalDirection = VerticalAirDirection.Up });


            taps = new Tap[]
            {
                new Tap() { Tick = 960, LaneIndex = 5, Width = 4 },
                new ExTap() { Tick = 960, LaneIndex = 9, Width = 3 },
                new Tap() { Tick = 960, LaneIndex = 12, Width = 4 }
            };
            foreach (var note in taps)
            {
                Notes.Add(note);
                Notes.Add(new Air(note) { HorizontalDirection = HorizontalAirDirection.Right, VerticalDirection = VerticalAirDirection.Down });
            }

            Notes.Add(new Flick() { Tick = 480 * 4, LaneIndex = 0, Width = 8 });
            Notes.Add(new Hold() { StartTick = 480 * 4, Duration = 480 * 4, Width = 8, LaneIndex = 8 });

            for (int i = 0; i < 4; i++)
            {
                Notes.Add(new Tap() { Tick = 240 * (1 + i) + 480 * 4, LaneIndex = i * 4, Width = 4 });
            }

            var tap1 = new Tap() { Tick = 480 * 6, LaneIndex = 0, Width = 3 };
            Notes.Add(new Air(tap1));
            var airaction1 = new AirAction(tap1);
            airaction1.ActionNotes.Add(new AirAction.ActionNote(airaction1) { Offset = 480 });
            airaction1.ActionNotes.Add(new AirAction.ActionNote(airaction1) { Offset = 480 * 2 });

            var tap2 = new Tap() { Tick = 480 * 7, LaneIndex = 0, Width = 3 };
            var air2 = new Air(tap2) { VerticalDirection = VerticalAirDirection.Down };
            Notes.Add(tap2);
            Notes.Add(air2);
            Notes.Add(tap1);
            Notes.Add(airaction1);

            var slide1 = new Slide() { Width = 4, StartTick = 480 * 4, StartLaneIndex = 8 };
            slide1.StepNotes.Add(new Slide.StepTap(slide1) { TickOffset = 240, LaneIndexOffset = 4 });
            slide1.StepNotes.Add(new Slide.StepTap(slide1) { TickOffset = 240 * 2, LaneIndexOffset = 0 });
            slide1.StepNotes.Add(new Slide.StepTap(slide1) { TickOffset = 240 * 3, LaneIndexOffset = 4 });
            slide1.StepNotes.Add(new Slide.StepTap(slide1) { TickOffset = 240 * 4, LaneIndexOffset = 0 });
            slide1.StepNotes.Add(new Slide.StepTap(slide1) { TickOffset = 240 * 5, LaneIndexOffset = 4 });
            Notes.Add(slide1);

            HeadTick = 240;

            var mouseDown = this.MouseDownAsObservable();
            var mouseMove = this.MouseMoveAsObservable();
            var mouseUp = this.MouseUpAsObservable();

            mouseDown
                .Where(p => p.Button == MouseButtons.Left)
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

                    Func<AirAction.ActionNote, IObservable<MouseEventArgs>> actionNoteHandler = action =>
                    {
                        var offsets = new HashSet<int>(action.ParentNote.ActionNotes.Select(q => q.Offset));
                        offsets.Remove(action.Offset);
                        return mouseMove
                            .TakeUntil(mouseUp)
                            .Do(q =>
                            {
                                var currentScorePos = matrix.TransformPoint(q.Location);
                                int offset = GetQuantizedTick(GetTickFromYPosition(currentScorePos.Y)) - action.ParentNote.ParentNote.Tick;
                                if (offset <= 0 || offsets.Contains(offset)) return;
                                action.Offset = offset;
                            });
                    };

                    // AIR-ACTION
                    foreach (var note in Notes.AirActions)
                    {
                        foreach (var action in note.ActionNotes)
                        {
                            RectangleF noteRect = GetRectFromNotePosition(note.ParentNote.Tick + action.Offset, note.ParentNote.LaneIndex, note.ParentNote.Width);
                            if (noteRect.Contains(scorePos))
                            {
                                int beforeOffset = action.Offset;
                                return actionNoteHandler(action)
                                    .Finally(() =>
                                    {
                                        OperationManager.Push(new ChangeAirActionOffsetOperation(action, beforeOffset, action.Offset));
                                    });
                            }
                        }
                    }

                    Func<TappableBase, IObservable<MouseEventArgs>> moveTappableNoteHandler = note =>
                    {
                        int beforeLaneIndex = note.LaneIndex;
                        return mouseMove
                            .TakeUntil(mouseUp)
                            .Do(q =>
                            {
                                var currentScorePos = matrix.TransformPoint(q.Location);
                                note.Tick = GetQuantizedTick(GetTickFromYPosition(currentScorePos.Y));
                                int xdiff = (int)((currentScorePos.X - scorePos.X) / (UnitLaneWidth + BorderThickness));
                                int laneIndex = beforeLaneIndex + xdiff;
                                note.LaneIndex = Math.Min(Constants.LanesCount - note.Width, Math.Max(0, laneIndex));
                            });
                    };

                    Func<TappableBase, IObservable<MouseEventArgs>> tappableNoteLeftThumbHandler = note =>
                    {
                        var beforePos = new ChangeShortNoteWidthOperation.NotePosition(note.LaneIndex, note.Width);
                        return mouseMove
                            .TakeUntil(mouseUp)
                            .Do(q =>
                            {
                                var currentScorePos = matrix.TransformPoint(q.Location);
                                int xdiff = (int)((currentScorePos.X - scorePos.X) / (UnitLaneWidth + BorderThickness));
                                int startx = (int)(scorePos.X / (UnitLaneWidth + BorderThickness));
                                xdiff = Math.Min(beforePos.Width - 1, Math.Max(-startx, xdiff));
                                int width = beforePos.Width - xdiff;
                                int laneIndex = beforePos.LaneIndex + xdiff;
                                //System.Diagnostics.Debug.WriteLine("xdiff: {0}, width: {1}, laneIndex: {2}", xdiff, width, laneIndex);
                                note.Width = Math.Min(Constants.LanesCount - note.LaneIndex, Math.Max(1, width));
                                note.LaneIndex = Math.Min(Constants.LanesCount - note.Width, Math.Max(0, laneIndex));
                            });
                    };

                    Func<TappableBase, IObservable<MouseEventArgs>> tappableNoteRightThumbHandler = note =>
                    {
                        int beforeWidth = note.Width;
                        return mouseMove
                            .TakeUntil(mouseUp)
                            .Do(q =>
                            {
                                var currentScorePos = matrix.TransformPoint(q.Location);
                                int xdiff = (int)((currentScorePos.X - scorePos.X) / (UnitLaneWidth + BorderThickness));
                                int width = beforeWidth + xdiff;
                                note.Width = Math.Min(Constants.LanesCount - note.LaneIndex, Math.Max(1, width));
                            });
                    };


                    Func<TappableBase, IObservable<MouseEventArgs>> shortNoteHandler = note =>
                    {
                        RectangleF rect = GetRectFromNotePosition(note.Tick, note.LaneIndex, note.Width);
                        RectangleF leftThumb = new RectangleF(rect.X, rect.Y, rect.Width * 0.2f, rect.Height);
                        RectangleF rightThumb = new RectangleF(rect.Right - rect.Width * 0.2f, rect.Y, rect.Width * 0.2f, rect.Height);
                        // ノートの左側
                        if (leftThumb.Contains(scorePos))
                        {
                            var beforePos = new ChangeShortNoteWidthOperation.NotePosition(note.LaneIndex, note.Width);
                            return tappableNoteLeftThumbHandler(note)
                                .Finally(() =>
                                {
                                    var afterPos = new ChangeShortNoteWidthOperation.NotePosition(note.LaneIndex, note.Width);
                                    OperationManager.Push(new ChangeShortNoteWidthOperation(note, beforePos, afterPos));
                                });
                        }

                        // ノートの右側
                        if (rightThumb.Contains(scorePos))
                        {
                            var beforePos = new ChangeShortNoteWidthOperation.NotePosition(note.LaneIndex, note.Width);
                            return tappableNoteRightThumbHandler(note)
                                .Finally(() =>
                                {
                                    var afterPos = new ChangeShortNoteWidthOperation.NotePosition(note.LaneIndex, note.Width);
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
                                    OperationManager.Push(new MoveShortNoteOperation(note, beforePos, afterPos));
                                });
                        }

                        return null;
                    };

                    if (!(NoteType.Air | NoteType.AirAction).HasFlag(NewNoteType))
                    {
                        foreach (var note in Notes.Taps.Where(q => q.Tick >= HeadTick && q.Tick <= tailTick))
                        {
                            var subscription = shortNoteHandler(note);
                            if (subscription != null) return subscription;
                        }

                        foreach (var note in Notes.Flicks.Where(q => q.Tick >= HeadTick && q.Tick <= tailTick))
                        {
                            var subscription = shortNoteHandler(note);
                            if (subscription != null) return subscription;
                        }

                        foreach (var note in Notes.Damages.Where(q => q.Tick >= HeadTick && q.Tick <= tailTick))
                        {
                            var subscription = shortNoteHandler(note);
                            if (subscription != null) return subscription;
                        }
                    }

                    Func<Hold, IObservable<MouseEventArgs>> holdDurationHandler = hold =>
                    {
                        return mouseMove.TakeUntil(mouseUp)
                            .Do(q =>
                            {
                                var currentCursorPos = matrix.TransformPoint(q.Location);
                                hold.Duration = Math.Max(QuantizeTick, GetQuantizedTick(GetTickFromYPosition(currentCursorPos.Y)) - hold.StartTick);
                            });
                    };

                    Func<Slide.StepTap, IObservable<MouseEventArgs>> slideStepNoteHandler = step =>
                    {
                        int beforeLaneIndexOffset = step.LaneIndexOffset;
                        var offsets = new HashSet<int>(step.ParentNote.StepNotes.Select(q => q.TickOffset));
                        int maxOffset = offsets.Max();
                        bool isMaxOffsetStep = step.TickOffset == maxOffset;
                        offsets.Remove(step.TickOffset);
                        return mouseMove
                            .TakeUntil(mouseUp)
                            .Do(q =>
                            {
                                var currentScorePos = matrix.TransformPoint(q.Location);
                                int offset = GetQuantizedTick(GetTickFromYPosition(currentScorePos.Y)) - step.ParentNote.StartTick;
                                int xdiff = (int)((currentScorePos.X - scorePos.X) / (UnitLaneWidth + BorderThickness));
                                int laneIndexOffset = beforeLaneIndexOffset + xdiff;
                                step.LaneIndexOffset = Math.Min(Constants.LanesCount - step.ParentNote.Width - step.ParentNote.StartLaneIndex, Math.Max(-step.ParentNote.StartLaneIndex, laneIndexOffset));
                                // 最終Step以降に移動はさせないし同じTickに置かせもしない
                                if ((!isMaxOffsetStep && offset > maxOffset) || offsets.Contains(offset) || offset <= 0) return;
                                step.TickOffset = offset;
                            });
                    };

                    foreach (var note in Notes.Slides.Where(q => q.StartTick <= tailTick && q.StartTick + q.GetDuration() >= HeadTick))
                    {
                        foreach (var step in note.StepNotes)
                        {
                            RectangleF stepRect = GetRectFromNotePosition(step.Tick, step.LaneIndex, step.Width);
                            if (stepRect.Contains(scorePos))
                            {
                                var beforeStepPos = new MoveSlideStepNoteOperation.NotePosition(step.TickOffset, step.LaneIndexOffset);
                                return slideStepNoteHandler(step)
                                    .Finally(() =>
                                    {
                                        var afterPos = new MoveSlideStepNoteOperation.NotePosition(step.TickOffset, step.LaneIndexOffset);
                                        OperationManager.Push(new MoveSlideStepNoteOperation(step, beforeStepPos, afterPos));
                                    });
                            }
                        }

                        RectangleF startRect = GetRectFromNotePosition(note.StartNote.Tick, note.StartNote.LaneIndex, note.StartNote.Width);
                        RectangleF leftThumbRect = new RectangleF(startRect.Left, startRect.Top, startRect.Width * 0.2f, startRect.Height);
                        RectangleF rightThumbRect = new RectangleF(startRect.Right - startRect.Width * 0.2f, startRect.Top, startRect.Width * 0.2f, startRect.Height);

                        int leftStepLaneIndexOffset = Math.Min(0, note.StepNotes.Min(q => q.LaneIndexOffset));
                        int rightStepLaneIndexOffset = Math.Max(0, note.StepNotes.Max(q => q.LaneIndexOffset));

                        var beforePos = new MoveSlideOperation.NotePosition(note.StartTick, note.StartLaneIndex, note.Width);
                        if (leftThumbRect.Contains(scorePos))
                        {
                            return mouseMove
                                .TakeUntil(mouseUp)
                                .Do(q =>
                                {
                                    var currentScorePos = matrix.TransformPoint(q.Location);
                                    int xdiff = (int)((currentScorePos.X - scorePos.X) / (UnitLaneWidth + BorderThickness));
                                    xdiff = Math.Min(beforePos.Width - 1, Math.Max(-beforePos.StartLaneIndex - leftStepLaneIndexOffset, xdiff));
                                    int width = beforePos.Width - xdiff;
                                    int laneIndex = beforePos.StartLaneIndex + xdiff;
                                    note.StartLaneIndex = Math.Min(Constants.LanesCount - note.Width - rightStepLaneIndexOffset, Math.Max(-leftStepLaneIndexOffset, laneIndex));
                                    note.Width = Math.Min(Constants.LanesCount - note.StartLaneIndex - rightStepLaneIndexOffset, Math.Max(1, width));
                                })
                                .Finally(() =>
                                {
                                    var afterPos = new MoveSlideOperation.NotePosition(note.StartTick, note.StartLaneIndex, note.Width);
                                    OperationManager.Push(new MoveSlideOperation(note, beforePos, afterPos));
                                });
                        }

                        if (rightThumbRect.Contains(scorePos))
                        {
                            return mouseMove
                                .TakeUntil(mouseUp)
                                .Do(q =>
                                {
                                    var currentScorePos = matrix.TransformPoint(q.Location);
                                    int xdiff = (int)((currentScorePos.X - scorePos.X) / (UnitLaneWidth + BorderThickness));
                                    int width = beforePos.Width + xdiff;
                                    note.Width = Math.Min(Constants.LanesCount - note.StartLaneIndex - rightStepLaneIndexOffset, Math.Max(1, width));
                                })
                                .Finally(() =>
                                {
                                    var afterPos = new MoveSlideOperation.NotePosition(note.StartTick, note.StartLaneIndex, note.Width);
                                    OperationManager.Push(new MoveSlideOperation(note, beforePos, afterPos));
                                });
                        }

                        if (startRect.Contains(scorePos))
                        {
                            int beforeLaneIndex = note.StartNote.LaneIndex;
                            return mouseMove
                                .TakeUntil(mouseUp)
                                .Do(q =>
                                {
                                    var currentScorePos = matrix.TransformPoint(q.Location);
                                    note.StartTick = GetQuantizedTick(GetTickFromYPosition(currentScorePos.Y));
                                    int xdiff = (int)((currentScorePos.X - scorePos.X) / (UnitLaneWidth + BorderThickness));
                                    int laneIndex = beforeLaneIndex + xdiff;
                                    note.StartLaneIndex = Math.Min(Constants.LanesCount - note.Width - rightStepLaneIndexOffset, Math.Max(-leftStepLaneIndexOffset, laneIndex));
                                })
                                .Finally(() =>
                                {
                                    var afterPos = new MoveSlideOperation.NotePosition(note.StartTick, note.StartLaneIndex, note.Width);
                                    OperationManager.Push(new MoveSlideOperation(note, beforePos, afterPos));
                                });
                        }
                    }

                    foreach (var note in Notes.Holds.Where(q => q.StartTick <= tailTick && q.StartTick + q.GetDuration() >= HeadTick))
                    {
                        // HOLD長さ変更
                        if (GetRectFromNotePosition(note.EndNote.Tick, note.LaneIndex, note.Width).Contains(scorePos))
                        {
                            int beforeDuration = note.Duration;
                            return holdDurationHandler(note)
                                .Finally(() => OperationManager.Push(new ChangeHoldDurationOperation(note, beforeDuration, note.Duration)));
                        }

                        RectangleF startRect = GetRectFromNotePosition(note.StartTick, note.LaneIndex, note.Width);
                        RectangleF leftThumbRect = new RectangleF(startRect.Left, startRect.Top, startRect.Width * 0.2f, startRect.Height);
                        RectangleF rightThumbRect = new RectangleF(startRect.Right - startRect.Width * 0.2f, startRect.Top, startRect.Width * 0.2f, startRect.Height);

                        var beforePos = new ChangeHoldPositionOperation.NotePosition(note.StartTick, note.LaneIndex, note.Width);
                        if (leftThumbRect.Contains(scorePos))
                        {
                            return mouseMove
                                .TakeUntil(mouseUp)
                                .Do(q =>
                                {
                                    var currentScorePos = matrix.TransformPoint(q.Location);
                                    int xdiff = (int)((currentScorePos.X - scorePos.X) / (UnitLaneWidth + BorderThickness));
                                    int startx = (int)(scorePos.X / (UnitLaneWidth + BorderThickness));
                                    xdiff = Math.Min(beforePos.Width - 1, Math.Max(-startx, xdiff));
                                    int width = beforePos.Width - xdiff;
                                    int laneIndex = beforePos.LaneIndex + xdiff;
                                    note.Width = Math.Min(Constants.LanesCount - note.LaneIndex, Math.Max(1, width));
                                    note.LaneIndex = Math.Min(Constants.LanesCount - note.Width, Math.Max(0, laneIndex));
                                })
                                .Finally(() =>
                                {
                                    var afterPos = new ChangeHoldPositionOperation.NotePosition(note.StartTick, note.LaneIndex, note.Width);
                                    OperationManager.Push(new ChangeHoldPositionOperation(note, beforePos, afterPos));
                                });
                        }

                        if (rightThumbRect.Contains(scorePos))
                        {
                            return mouseMove
                                .TakeUntil(mouseUp)
                                .Do(q =>
                                {
                                    var currentScorePos = matrix.TransformPoint(q.Location);
                                    int xdiff = (int)((currentScorePos.X - scorePos.X) / (UnitLaneWidth + BorderThickness));
                                    int width = beforePos.Width + xdiff;
                                    note.Width = Math.Min(Constants.LanesCount - note.LaneIndex, Math.Max(1, width));
                                })
                                .Finally(() =>
                                {
                                    var afterPos = new ChangeHoldPositionOperation.NotePosition(note.StartTick, note.LaneIndex, note.Width);
                                    OperationManager.Push(new ChangeHoldPositionOperation(note, beforePos, afterPos));
                                });
                        }

                        if (startRect.Contains(scorePos))
                        {
                            return mouseMove
                                .TakeUntil(mouseUp)
                                .Do(q =>
                                {
                                    var currentScorePos = matrix.TransformPoint(q.Location);
                                    note.StartTick = GetQuantizedTick(GetTickFromYPosition(currentScorePos.Y));
                                    int xdiff = (int)((currentScorePos.X - scorePos.X) / (UnitLaneWidth + BorderThickness));
                                    int laneIndex = beforePos.LaneIndex + xdiff;
                                    note.LaneIndex = Math.Min(Constants.LanesCount - note.Width, Math.Max(0, laneIndex));
                                })
                                .Finally(() =>
                                {
                                    var afterPos = new ChangeHoldPositionOperation.NotePosition(note.StartTick, note.LaneIndex, note.Width);
                                    OperationManager.Push(new ChangeHoldPositionOperation(note, beforePos, afterPos));
                                });
                        }
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
                                op = new InsertTapOperation(Notes, extap);
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
                        newNote.Width = 4;
                        newNote.Tick = GetQuantizedTick(GetTickFromYPosition(scorePos.Y));
                        int newNoteLaneIndex = (int)(scorePos.X / (UnitLaneWidth + BorderThickness)) - newNote.Width / 2;
                        newNoteLaneIndex = Math.Min(Constants.LanesCount - newNote.Width, Math.Max(0, newNoteLaneIndex));
                        newNote.LaneIndex = newNoteLaneIndex;
                        Invalidate();
                        return moveTappableNoteHandler(newNote)
                            .Finally(() => OperationManager.Push(op));
                    }
                    else
                    {
                        int newNoteLaneIndex;
                        IEnumerable<TappableBase> tappables = Enumerable.Empty<TappableBase>();
                        tappables = tappables.Concat(Notes.Taps);
                        tappables = tappables.Concat(Notes.Flicks);
                        tappables = tappables.Concat(Notes.Damages);

                        switch (NewNoteType)
                        {
                            case NoteType.Hold:
                                var hold = new Hold
                                {
                                    StartTick = GetQuantizedTick(GetTickFromYPosition(scorePos.Y)),
                                    Width = 4,
                                    Duration = QuantizeTick
                                };
                                newNoteLaneIndex = (int)(scorePos.X / (UnitLaneWidth + BorderThickness)) - hold.Width / 2;
                                hold.LaneIndex = Math.Min(Constants.LanesCount - hold.Width, Math.Max(0, newNoteLaneIndex));
                                Notes.Add(hold);
                                Invalidate();
                                return holdDurationHandler(hold)
                                    .Finally(() => OperationManager.Push(new InsertHoldOperation(Notes, hold)));

                            case NoteType.Slide:
                                var slide = new Slide()
                                {
                                    StartTick = GetQuantizedTick(GetTickFromYPosition(scorePos.Y)),
                                    Width = 4
                                };
                                newNoteLaneIndex = (int)(scorePos.X / (UnitLaneWidth + BorderThickness)) - slide.Width / 2;
                                slide.StartLaneIndex = Math.Min(Constants.LanesCount - slide.Width, Math.Max(0, newNoteLaneIndex));
                                var step = new Slide.StepTap(slide) { TickOffset = QuantizeTick };
                                slide.StepNotes.Add(step);
                                Notes.Add(slide);
                                Invalidate();
                                return slideStepNoteHandler(step)
                                    .Finally(() => OperationManager.Push(new InsertSlideOperation(Notes, slide)));

                            case NoteType.Air:
                                foreach (var note in tappables)
                                {
                                    RectangleF rect = GetRectFromNotePosition(note.Tick, note.LaneIndex, note.Width);
                                    if (rect.Contains(scorePos))
                                    {
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
                                break;

                            case NoteType.AirAction:
                                foreach (var note in tappables)
                                {
                                    RectangleF rect = GetRectFromNotePosition(note.Tick, note.LaneIndex, note.Width);
                                    if (rect.Contains(scorePos))
                                    {
                                        var airAction = new AirAction(note);
                                        var action = new AirAction.ActionNote(airAction) { Offset = QuantizeTick };
                                        airAction.ActionNotes.Add(action);
                                        Notes.Add(airAction);
                                        return actionNoteHandler(action)
                                            .Finally(() => OperationManager.Push(new InsertAirActionOperation(Notes, airAction)));
                                    }
                                }
                                break;
                        }
                    }
                    return Observable.Empty<MouseEventArgs>();
                }).Subscribe(p => Invalidate());

            mouseDown
                .Where(p => p.Button == MouseButtons.Right)
                .SelectMany(p => mouseMove
                    .TakeUntil(mouseUp)
                    .Count()
                    .Zip(mouseUp, (q, r) => new { Pos = r.Location, Count = q })
                )
                .Where(p => p.Count == 0) // ドラッグなし
                .Do(p =>
                {
                    Matrix matrix = GetDrawingMatrix(new Matrix());
                    matrix.Invert();
                    PointF scorePos = matrix.TransformPoint(p.Pos);

                    foreach (var note in Notes.Airs)
                    {
                        RectangleF rect = note.GetDestRectangle(GetRectFromNotePosition(note.ParentNote.Tick, note.ParentNote.LaneIndex, note.ParentNote.Width));
                        if (rect.Contains(scorePos))
                        {
                            Notes.Remove(note);
                            OperationManager.Push(new RemoveAirOperation(Notes, note));
                            return;
                        }
                    }

                    foreach (var note in Notes.AirActions)
                    {
                        foreach (var action in note.ActionNotes)
                        {
                            RectangleF rect = GetRectFromNotePosition(note.StartTick + action.Offset, note.ParentNote.LaneIndex, note.ParentNote.Width);
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

                    foreach (var note in Notes.Flicks)
                    {
                        RectangleF rect = GetRectFromNotePosition(note.Tick, note.LaneIndex, note.Width);
                        if (rect.Contains(scorePos))
                        {
                            Notes.Remove(note);
                            OperationManager.Push(new RemoveFlickOperation(Notes, note));
                            return;
                        }
                    }

                    foreach (var note in Notes.Damages)
                    {
                        RectangleF rect = GetRectFromNotePosition(note.Tick, note.LaneIndex, note.Width);
                        if (rect.Contains(scorePos))
                        {
                            Notes.Remove(note);
                            OperationManager.Push(new RemoveDamageOperation(Notes, note));
                            return;
                        }
                    }

                    foreach (var note in Notes.Taps)
                    {
                        RectangleF rect = GetRectFromNotePosition(note.Tick, note.LaneIndex, note.Width);
                        if (rect.Contains(scorePos))
                        {
                            Notes.Remove(note);
                            OperationManager.Push(new RemoveTapOperation(Notes, note));
                            return;
                        }
                    }

                    foreach (var slide in Notes.Slides)
                    {
                        foreach (var step in slide.StepNotes)
                        {
                            RectangleF rect = GetRectFromNotePosition(step.Tick, step.LaneIndex, step.Width);
                            if (rect.Contains(scorePos))
                            {
                                slide.StepNotes.Remove(step);
                                OperationManager.Push(new RemoveSlideStepNoteOperation(slide, step));
                                return;
                            }
                        }

                        RectangleF startRect = GetRectFromNotePosition(slide.StartTick, slide.StartLaneIndex, slide.Width);
                        if (startRect.Contains(scorePos))
                        {
                            Notes.Remove(slide);
                            OperationManager.Push(new RemoveSlideOperation(Notes, slide));
                            return;
                        }
                    }

                    foreach (var hold in Notes.Holds)
                    {
                        RectangleF rect = GetRectFromNotePosition(hold.StartTick, hold.LaneIndex, hold.Width);
                        if (rect.Contains(scorePos))
                        {
                            Notes.Remove(hold);
                            OperationManager.Push(new RemoveHoldOperation(Notes, hold));
                            return;
                        }
                    }
                })
                .Subscribe(p => Invalidate());
        }

        protected override void OnPaint(PaintEventArgs pe)
        {
            base.OnPaint(pe);

            // Y軸の正方向をTick増加方向として描画 (y = 0 はコントロール下端)
            // コントロールの中心に描画したいなら後でTranslateしといてね
            var prevMatrix = pe.Graphics.Transform;
            pe.Graphics.Transform = GetDrawingMatrix(prevMatrix);

            float laneWidth = LaneWidth;
            int tailTick = HeadTick + (int)(ClientSize.Height * UnitBeatTick / UnitBeatHeight);

            // レーン分割線描画
            using (var pen = new Pen(LaneBorderColor, BorderThickness))
            {
                for (int i = 0; i <= Constants.LanesCount; i++)
                {
                    float x = i * (UnitLaneWidth + BorderThickness);
                    pe.Graphics.DrawLine(pen, x, GetYPositionFromTick(HeadTick), x, GetYPositionFromTick(tailTick));
                }
            }


            // 時間ガイドの描画
            using (var beatPen = new Pen(BeatLineColor, BorderThickness))
            using (var barPen = new Pen(BarLineColor, BorderThickness))
            {
                for (int i = HeadTick / UnitBeatTick; i * UnitBeatTick < tailTick; i++)
                {
                    float y = i * UnitBeatHeight;
                    // 4分の4拍子で1小節ごとに小節区切り
                    pe.Graphics.DrawLine(i % 4 == 0 ? barPen : beatPen, 0, y, laneWidth, y);
                }
            }

            pe.Graphics.DrawLine(Pens.Red, 0, 0, laneWidth, 0);

            // ノート描画
            var holds = Notes.Holds.Where(p => p.StartTick <= tailTick && p.StartTick + p.GetDuration() >= HeadTick).ToList();
            // ロングノーツ背景
            // HOLD
            foreach (var hold in holds)
            {
                hold.DrawBackground(pe.Graphics, new RectangleF(
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
                for (int i = 0; i < bg.Count - 1; i++)
                {
                    slide.DrawBackground(pe.Graphics,
                        (UnitLaneWidth + BorderThickness) * slide.Width - BorderThickness,
                        (UnitLaneWidth + BorderThickness) * bg[i].LaneIndex,
                        GetYPositionFromTick(bg[i].Tick),
                        (UnitLaneWidth + BorderThickness) * bg[i + 1].LaneIndex,
                        GetYPositionFromTick(bg[i + 1].Tick),
                        ShortNoteHeight);
                }
            }

            // AIR-ACTION(ガイド線)
            var airActions = Notes.AirActions.Where(p => p.StartTick <= tailTick && p.StartTick + p.GetDuration() >= HeadTick).ToList();
            foreach (var note in airActions)
            {
                note.DrawLine(pe.Graphics,
                    (UnitLaneWidth + BorderThickness) * note.ParentNote.LaneIndex + BorderThickness + UnitLaneWidth * note.ParentNote.Width / 2,
                    GetYPositionFromTick(note.StartTick),
                    GetYPositionFromTick(note.StartTick + note.GetDuration()),
                    ShortNoteHeight);
            }

            // ショートノーツ
            // HOLD始点
            foreach (var hold in holds)
            {
                hold.StartNote.Draw(pe.Graphics, GetRectFromNotePosition(hold.StartTick, hold.LaneIndex, hold.Width));
                hold.EndNote.Draw(pe.Graphics, GetRectFromNotePosition(hold.StartTick + hold.Duration, hold.LaneIndex, hold.Width));
            }

            // SLIDE始点
            foreach (var slide in slides)
            {
                slide.StartNote.Draw(pe.Graphics, GetRectFromNotePosition(slide.StartTick, slide.StartNote.LaneIndex, slide.Width));
                foreach (var step in slide.StepNotes)
                {
                    if (!step.IsVisible) continue;
                    step.Draw(pe.Graphics, GetRectFromNotePosition(step.Tick, step.LaneIndex, step.Width));
                }
            }

            // TAP, ExTAP, FLICK, DAMAGE
            foreach (var note in Notes.Taps.Where(p => p.Tick >= HeadTick && p.Tick <= tailTick))
            {
                note.Draw(pe.Graphics, GetRectFromNotePosition(note.Tick, note.LaneIndex, note.Width));
            }

            foreach (var note in Notes.Flicks.Where(p => p.Tick >= HeadTick && p.Tick <= tailTick))
            {
                note.Draw(pe.Graphics, GetRectFromNotePosition(note.Tick, note.LaneIndex, note.Width));
            }

            foreach (var note in Notes.Damages.Where(p => p.Tick >= HeadTick && p.Tick <= tailTick))
            {
                note.Draw(pe.Graphics, GetRectFromNotePosition(note.Tick, note.LaneIndex, note.Width));
            }

            // AIR-ACTION(ActionNote)
            foreach (var action in airActions)
            {
                foreach (var note in action.ActionNotes)
                {
                    note.Draw(pe.Graphics, GetRectFromNotePosition(action.StartTick + note.Offset, action.ParentNote.LaneIndex, action.ParentNote.Width).Expand(-ShortNoteHeight * 0.28f));
                }
            }

            // AIR
            foreach (var note in Notes.Airs.Where(p => p.Tick >= HeadTick && p.Tick <= tailTick))
            {
                note.Draw(pe.Graphics, GetRectFromNotePosition(note.ParentNote.Tick, note.ParentNote.LaneIndex, note.ParentNote.Width));
            }

            // Y軸反転させずにTick = 0をY軸原点とする座標系へ
            pe.Graphics.Transform = GetDrawingMatrix(prevMatrix, false);

            Font font = new Font("MS Gothic", 8);
            SizeF strSize = pe.Graphics.MeasureString("000", font);

            // 小節番号描画(4分の4拍子)
            for (int i = HeadTick / UnitBeatTick / 4; i * UnitBeatTick * 4 <= tailTick; i++)
            {
                var point = new PointF(-strSize.Width, -GetYPositionFromTick(i * UnitBeatTick * 4) - strSize.Height);
                pe.Graphics.DrawString(string.Format("{0:000}", i + 1), font, Brushes.White, point);
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

        protected int GetQuantizedTick(float tick)
        {
            return (int)Math.Round((float)tick / QuantizeTick) * QuantizeTick;
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

        public class NoteCollection
        {
            public event EventHandler NoteChanged;

            private List<Tap> taps;
            public IReadOnlyCollection<Tap> Taps { get { return taps; } }

            private List<Hold> holds;
            public IReadOnlyCollection<Hold> Holds { get { return holds; } }

            private List<Slide> slides;
            public IReadOnlyCollection<Slide> Slides { get { return slides; } }

            private List<Air> airs;
            public IReadOnlyCollection<Air> Airs { get { return airs; } }

            private List<AirAction> airActions;
            public IReadOnlyCollection<AirAction> AirActions { get { return airActions; } }

            private List<Flick> flicks;
            public IReadOnlyCollection<Flick> Flicks { get { return flicks; } }

            private List<Damage> damages;
            public IReadOnlyCollection<Damage> Damages { get { return damages; } }

            public NoteCollection()
            {
                taps = new List<Tap>();
                holds = new List<Hold>();
                slides = new List<Slide>();
                airs = new List<Air>();
                airActions = new List<AirAction>();
                flicks = new List<Flick>();
                damages = new List<Damage>();
            }

            public void Add(Tap note)
            {
                taps.Add(note);
                NoteChanged?.Invoke(this, EventArgs.Empty);
            }

            public void Add(Hold note)
            {
                holds.Add(note);
                NoteChanged?.Invoke(this, EventArgs.Empty);
            }

            public void Add(Slide note)
            {
                slides.Add(note);
                NoteChanged?.Invoke(this, EventArgs.Empty);
            }

            public void Add(Air note)
            {
                airs.Add(note);
                NoteChanged?.Invoke(this, EventArgs.Empty);
            }

            public void Add(AirAction note)
            {
                airActions.Add(note);
                NoteChanged?.Invoke(this, EventArgs.Empty);
            }

            public void Add(Flick note)
            {
                flicks.Add(note);
                NoteChanged?.Invoke(this, EventArgs.Empty);
            }

            public void Add(Damage note)
            {
                damages.Add(note);
                NoteChanged?.Invoke(this, EventArgs.Empty);
            }


            public void Remove(Tap note)
            {
                taps.Remove(note);
                NoteChanged?.Invoke(this, EventArgs.Empty);
            }

            public void Remove(Hold note)
            {
                holds.Remove(note);
                NoteChanged?.Invoke(this, EventArgs.Empty);
            }

            public void Remove(Slide note)
            {
                slides.Remove(note);
                NoteChanged?.Invoke(this, EventArgs.Empty);
            }

            public void Remove(Air note)
            {
                airs.Remove(note);
                NoteChanged?.Invoke(this, EventArgs.Empty);
            }

            public void Remove(AirAction note)
            {
                airActions.Remove(note);
                NoteChanged?.Invoke(this, EventArgs.Empty);
            }

            public void Remove(Flick note)
            {
                flicks.Remove(note);
                NoteChanged?.Invoke(this, EventArgs.Empty);
            }

            public void Remove(Damage note)
            {
                damages.Remove(note);
                NoteChanged?.Invoke(this, EventArgs.Empty);
            }


            public void Clear()
            {
                taps.Clear();
                holds.Clear();
                slides.Clear();
                airs.Clear();
                airActions.Clear();
                flicks.Clear();
                damages.Clear();
                NoteChanged?.Invoke(this, EventArgs.Empty);
            }
        }
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
