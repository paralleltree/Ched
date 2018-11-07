using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Ched.Core.Notes;
using Ched.UI;

namespace Ched.UI.Operations
{
    public abstract class EditShortNoteOperation : IOperation
    {
        protected TappableBase Note { get; }
        public abstract string Description { get; }

        public EditShortNoteOperation(TappableBase note)
        {
            Note = note;
        }

        public abstract void Redo();
        public abstract void Undo();
    }

    public class MoveShortNoteOperation : EditShortNoteOperation
    {
        public override string Description { get { return "ノートを移動"; } }

        protected NotePosition BeforePosition { get; }
        protected NotePosition AfterPosition { get; }

        public MoveShortNoteOperation(TappableBase note, NotePosition before, NotePosition after) : base(note)
        {
            BeforePosition = before;
            AfterPosition = after;
        }

        public override void Redo()
        {
            Note.Tick = AfterPosition.Tick;
            Note.LaneIndex = AfterPosition.LaneIndex;
        }

        public override void Undo()
        {
            Note.Tick = BeforePosition.Tick;
            Note.LaneIndex = BeforePosition.LaneIndex;
        }

        public struct NotePosition
        {
            public int Tick { get; }
            public int LaneIndex { get; }

            public NotePosition(int tick, int laneIndex)
            {
                Tick = tick;
                LaneIndex = laneIndex;
            }

            public override bool Equals(object obj)
            {
                if (obj == null || !(obj is NotePosition)) return false;
                NotePosition other = (NotePosition)obj;
                return Tick == other.Tick && LaneIndex == other.LaneIndex;
            }

            public override int GetHashCode()
            {
                return Tick ^ LaneIndex;
            }

            public static bool operator ==(NotePosition a, NotePosition b)
            {
                return a.Equals(b);
            }

            public static bool operator !=(NotePosition a, NotePosition b)
            {
                return !a.Equals(b);
            }
        }
    }

    public class ChangeShortNoteWidthOperation : EditShortNoteOperation
    {
        public override string Description { get { return "ノート幅の変更"; } }

        protected NotePosition BeforePosition { get; }
        protected NotePosition AfterPosition { get; }

        public ChangeShortNoteWidthOperation(TappableBase note, NotePosition before, NotePosition after) : base(note)
        {
            BeforePosition = before;
            AfterPosition = after;
        }

        public override void Redo()
        {
            Note.SetPosition(AfterPosition.LaneIndex, AfterPosition.Width);
        }

        public override void Undo()
        {
            Note.SetPosition(BeforePosition.LaneIndex, BeforePosition.Width);
        }

        public struct NotePosition
        {
            public int LaneIndex { get; }
            public int Width { get; }

            public NotePosition(int laneIndex, int width)
            {
                LaneIndex = laneIndex;
                Width = width;
            }

            public override bool Equals(object obj)
            {
                if (obj == null || !(obj is NotePosition)) return false;
                NotePosition other = (NotePosition)obj;
                return LaneIndex == other.LaneIndex && Width == other.Width;
            }

            public override int GetHashCode()
            {
                return LaneIndex ^ Width;
            }

            public static bool operator ==(NotePosition a, NotePosition b)
            {
                return a.Equals(b);
            }

            public static bool operator !=(NotePosition a, NotePosition b)
            {
                return !a.Equals(b);
            }
        }
    }

    public class ChangeHoldDurationOperation : IOperation
    {
        public string Description { get { return "HOLD長さの変更"; } }

        protected Hold Note { get; }
        protected int BeforeDuration { get; }
        protected int AfterDuration { get; }

        public ChangeHoldDurationOperation(Hold note, int beforeDuration, int afterDuration)
        {
            Note = note;
            BeforeDuration = beforeDuration;
            AfterDuration = afterDuration;
        }

        public void Redo()
        {
            Note.Duration = AfterDuration;
        }

        public void Undo()
        {
            Note.Duration = BeforeDuration;
        }
    }

    public class MoveHoldOperation : IOperation
    {
        public string Description { get { return "HOLDの移動"; } }

        protected Hold Note { get; }
        protected NotePosition BeforePosition { get; }
        protected NotePosition AfterPosition { get; }

        public MoveHoldOperation(Hold note, NotePosition before, NotePosition after)
        {
            Note = note;
            BeforePosition = before;
            AfterPosition = after;
        }

        public void Redo()
        {
            Note.StartTick = AfterPosition.StartTick;
            Note.SetPosition(AfterPosition.LaneIndex, AfterPosition.Width);
        }

        public void Undo()
        {
            Note.StartTick = BeforePosition.StartTick;
            Note.SetPosition(BeforePosition.LaneIndex, BeforePosition.Width);
        }

        public struct NotePosition
        {
            public int StartTick { get; }
            public int LaneIndex { get; }
            public int Width { get; set; }

            public NotePosition(int startTick, int laneIndex, int width)
            {
                StartTick = startTick;
                LaneIndex = laneIndex;
                Width = width;
            }

            public override bool Equals(object obj)
            {
                if (obj == null || !(obj is NotePosition)) return false;
                NotePosition other = (NotePosition)obj;
                return StartTick == other.StartTick && LaneIndex == other.LaneIndex && Width == other.Width;
            }

            public override int GetHashCode()
            {
                return StartTick ^ LaneIndex ^ Width;
            }

            public static bool operator ==(NotePosition a, NotePosition b)
            {
                return a.Equals(b);
            }

            public static bool operator !=(NotePosition a, NotePosition b)
            {
                return !a.Equals(b);
            }
        }
    }

    public class MoveSlideStepNoteOperation : IOperation
    {
        public string Description { get { return "SLIDE中継点の移動"; } }

        public Slide.StepTap StepNote { get; }
        public NotePosition BeforePosition { get; }
        public NotePosition AfterPosition { get; }

        public MoveSlideStepNoteOperation(Slide.StepTap note, NotePosition before, NotePosition after)
        {
            StepNote = note;
            BeforePosition = before;
            AfterPosition = after;
        }

        public void Redo()
        {
            StepNote.TickOffset = AfterPosition.TickOffset;
            StepNote.SetPosition(AfterPosition.LaneIndexOffset, AfterPosition.WidthChange);
        }

        public void Undo()
        {
            StepNote.TickOffset = BeforePosition.TickOffset;
            StepNote.SetPosition(BeforePosition.LaneIndexOffset, BeforePosition.WidthChange);
        }

        public struct NotePosition
        {
            public int TickOffset { get; }
            public int LaneIndexOffset { get; }
            public int WidthChange { get; }

            public NotePosition(int tickOffset, int laneIndexOffset, int widthChange)
            {
                TickOffset = tickOffset;
                LaneIndexOffset = laneIndexOffset;
                WidthChange = widthChange;
            }

            public override bool Equals(object obj)
            {
                if (obj == null || !(obj is NotePosition)) return false;
                NotePosition other = (NotePosition)obj;
                return TickOffset == other.TickOffset && LaneIndexOffset == other.LaneIndexOffset && WidthChange == other.WidthChange;
            }

            public override int GetHashCode()
            {
                return TickOffset ^ LaneIndexOffset ^ WidthChange;
            }

            public static bool operator ==(NotePosition a, NotePosition b)
            {
                return a.Equals(b);
            }

            public static bool operator !=(NotePosition a, NotePosition b)
            {
                return !a.Equals(b);
            }
        }
    }

    public class MoveSlideOperation : IOperation
    {
        public string Description { get { return "SLIDEの移動"; } }

        protected Slide Note;
        protected NotePosition BeforePosition { get; }
        protected NotePosition AfterPosition { get; }

        public MoveSlideOperation(Slide note, NotePosition before, NotePosition after)
        {
            Note = note;
            BeforePosition = before;
            AfterPosition = after;
        }

        public void Redo()
        {
            Note.StartTick = AfterPosition.StartTick;
            Note.SetPosition(AfterPosition.StartLaneIndex, AfterPosition.StartWidth);
        }

        public void Undo()
        {
            Note.StartTick = BeforePosition.StartTick;
            Note.SetPosition(BeforePosition.StartLaneIndex, BeforePosition.StartWidth);
        }

        public struct NotePosition
        {
            public int StartTick { get; }
            public int StartLaneIndex { get; }
            public int StartWidth { get; }

            public NotePosition(int startTick, int startLaneIndex, int startWidth)
            {
                StartTick = startTick;
                StartLaneIndex = startLaneIndex;
                StartWidth = startWidth;
            }

            public override bool Equals(object obj)
            {
                if (obj == null || !(obj is NotePosition)) return false;
                NotePosition other = (NotePosition)obj;
                return StartTick == other.StartTick && StartLaneIndex == other.StartLaneIndex && StartWidth == other.StartWidth;
            }

            public override int GetHashCode()
            {
                return StartTick ^ StartLaneIndex ^ StartWidth;
            }

            public static bool operator ==(NotePosition a, NotePosition b)
            {
                return a.Equals(b);
            }

            public static bool operator !=(NotePosition a, NotePosition b)
            {
                return !a.Equals(b);
            }
        }
    }

    public class FlipSlideOperation : IOperation
    {
        public string Description { get { return "SLIDEの反転"; } }

        protected Slide Note;

        public FlipSlideOperation(Slide note)
        {
            Note = note;
        }

        public void Redo()
        {
            Note.Flip();
        }

        public void Undo()
        {
            Note.Flip();
        }
    }

    public abstract class SlideStepNoteCollectionOperation : IOperation
    {
        public abstract string Description { get; }

        protected Slide ParentNote { get; }
        protected Slide.StepTap StepNote { get; }

        public SlideStepNoteCollectionOperation(Slide parent, Slide.StepTap stepNote)
        {
            ParentNote = parent;
            StepNote = stepNote;
        }

        public abstract void Redo();
        public abstract void Undo();
    }

    public class InsertSlideStepNoteOperation : SlideStepNoteCollectionOperation
    {
        public override string Description { get { return "SLIDE中継点の追加"; } }

        public InsertSlideStepNoteOperation(Slide parent, Slide.StepTap stepNote) : base(parent, stepNote)
        {
        }

        public override void Redo()
        {
            ParentNote.StepNotes.Add(StepNote);
        }

        public override void Undo()
        {
            ParentNote.StepNotes.Remove(StepNote);
        }
    }

    public class RemoveSlideStepNoteOperation : SlideStepNoteCollectionOperation
    {
        public override string Description { get { return "SLIDE中継点の追加"; } }

        public RemoveSlideStepNoteOperation(Slide parent, Slide.StepTap stepNote) : base(parent, stepNote)
        {
        }

        public override void Redo()
        {
            ParentNote.StepNotes.Remove(StepNote);
        }

        public override void Undo()
        {
            ParentNote.StepNotes.Add(StepNote);
        }
    }

    public class FlipAirHorizontalDirectionOperation : IOperation
    {
        public string Description { get { return "AIRの反転"; } }

        protected Air Note { get; }

        public FlipAirHorizontalDirectionOperation(Air note)
        {
            Note = note;
        }

        public void Redo()
        {
            Note.Flip();
        }

        public void Undo()
        {
            Note.Flip();
        }
    }

    public class ChangeAirActionOffsetOperation : IOperation
    {
        public string Description { get { return "AIR-ACTION位置の変更"; } }

        protected AirAction.ActionNote Note { get; }
        protected int BeforeOffset { get; }
        protected int AfterOffset { get; }

        public ChangeAirActionOffsetOperation(AirAction.ActionNote note, int beforeOffset, int afterOffset)
        {
            Note = note;
            BeforeOffset = beforeOffset;
            AfterOffset = afterOffset;
        }

        public void Redo()
        {
            Note.Offset = AfterOffset;
        }

        public void Undo()
        {
            Note.Offset = BeforeOffset;
        }
    }

    public abstract class AirActionNoteOperationBase : IOperation
    {
        public abstract string Description { get; }

        protected AirAction ParentNote { get; }
        protected AirAction.ActionNote ActionNote { get; }

        public AirActionNoteOperationBase(AirAction parent, AirAction.ActionNote actionNote)
        {
            ParentNote = parent;
            ActionNote = actionNote;
        }

        public abstract void Redo();
        public abstract void Undo();
    }

    public class InsertAirActionNoteOperation : AirActionNoteOperationBase
    {
        public override string Description { get { return "AIR-ACTIONの追加"; } }

        public InsertAirActionNoteOperation(AirAction parent, AirAction.ActionNote actionNote) : base(parent, actionNote)
        {
        }

        public override void Redo()
        {
            ParentNote.ActionNotes.Add(ActionNote);
        }

        public override void Undo()
        {
            ParentNote.ActionNotes.Remove(ActionNote);
        }
    }

    public class RemoveAirActionNoteOperation : AirActionNoteOperationBase
    {
        public override string Description { get { return "AIR-ACTIONの追加"; } }

        public RemoveAirActionNoteOperation(AirAction parent, AirAction.ActionNote actionNote) : base(parent, actionNote)
        {
        }

        public override void Redo()
        {
            ParentNote.ActionNotes.Remove(ActionNote);
        }

        public override void Undo()
        {
            ParentNote.ActionNotes.Add(ActionNote);
        }
    }
}
