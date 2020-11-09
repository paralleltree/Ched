using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Reactive.Linq;
using System.Reactive.Concurrency;

using Ched.Core;
using Ched.Core.Notes;
using Ched.Drawing;
using Ched.UI.Operations;

namespace Ched.UI
{
    partial class NoteView
    {
        protected readonly IObservable<MouseEventArgs> mouseDown;
        protected readonly IObservable<MouseEventArgs> mouseMove;
        protected readonly IObservable<MouseEventArgs> mouseUp;

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

        private void InitializeHandlers()
        {
            // マウスをクリックしているとき以外
            var mouseMoveSubscription = mouseMove.TakeUntil(mouseDown).Concat(mouseMove.SkipUntil(mouseUp).TakeUntil(mouseDown).Repeat())
                .Where(p => EditMode == EditMode.Edit && Editable)
                .Do(HandleCursor)
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
                .SelectMany(HandleEdit)
                .Subscribe(p => Invalidate());

            var eraseSubscription = mouseDown
                .Where(p => Editable)
                .Where(p => p.Button == MouseButtons.Left && EditMode == EditMode.Erase)
                .SelectMany(p =>
                {
                    Matrix startMatrix = GetDrawingMatrix(new Matrix());
                    startMatrix.Invert();
                    PointF startScorePos = startMatrix.TransformPoint(p.Location);
                    return HandleRangeSelection(startScorePos)
                        .Count()
                        .Zip(mouseUp, (q, r) => new { Pos = r.Location, Count = q });
                })
                .Do(p => HandleErase(p.Pos, p.Count > 0))
                .Subscribe(p => Invalidate());

            var selectSubscription = mouseDown
                .Where(p => Editable)
                .Where(p => p.Button == MouseButtons.Left && EditMode == EditMode.Select)
                .SelectMany(HandleSelect)
                .Subscribe();

            Subscriptions.Add(mouseMoveSubscription);
            Subscriptions.Add(dragSubscription);
            Subscriptions.Add(editSubscription);
            Subscriptions.Add(eraseSubscription);
            Subscriptions.Add(selectSubscription);
        }

        protected IObservable<MouseEventArgs> HandleRangeSelection(PointF startPos)
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

        protected void HandleCursor(MouseEventArgs p)
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
        }

        protected IObservable<MouseEventArgs> HandleEdit(MouseEventArgs p)
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

                switch (NewNoteType)
                {
                    case NoteType.Hold:
                        var hold = new Hold
                        {
                            StartTick = Math.Max(GetQuantizedTick(GetTickFromYPosition(scorePos.Y)), 0),
                            Width = LastWidth,
                            Duration = (int)QuantizeTick
                        };
                        newNoteLaneIndex = (int)(scorePos.X / (UnitLaneWidth + BorderThickness)) - hold.Width / 2;
                        hold.LaneIndex = Math.Min(Constants.LanesCount - hold.Width, Math.Max(0, newNoteLaneIndex));
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
                        newNoteLaneIndex = (int)(scorePos.X / (UnitLaneWidth + BorderThickness)) - slide.StartWidth / 2;
                        slide.StartLaneIndex = Math.Min(Constants.LanesCount - slide.StartWidth, Math.Max(0, newNoteLaneIndex));
                        var step = new Slide.StepTap(slide) { TickOffset = (int)QuantizeTick };
                        slide.StepNotes.Add(step);
                        Notes.Add(slide);
                        Invalidate();
                        return moveSlideStepNoteHandler(step)
                            .Finally(() => OperationManager.Push(new InsertSlideOperation(Notes, slide)));
                }
            }
            return Observable.Empty<MouseEventArgs>();
        }

        protected void HandleErase(Point mouseUpPos, bool dragged)
        {
            {
                if (dragged) // ドラッグで範囲選択された
                {
                    RemoveSelectedNotes();
                    SelectedRange = SelectionRange.Empty;
                    return;
                }

                Matrix matrix = GetDrawingMatrix(new Matrix());
                matrix.Invert();
                PointF scorePos = matrix.TransformPoint(mouseUpPos);

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
            }
        }

        protected IObservable<MouseEventArgs> HandleSelect(MouseEventArgs p)
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
                return HandleRangeSelection(startScorePos);
            }
        }
    }
}
