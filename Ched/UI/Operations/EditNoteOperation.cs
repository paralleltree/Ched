using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Ched.Components;
using Ched.UI;

namespace Ched.UI.Operations
{
    internal abstract class EditShortNoteOperation : IOperation
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

    internal class MoveShortNoteOperation : EditShortNoteOperation
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

        public class NotePosition
        {
            public int Tick { get; }
            public int LaneIndex { get; }

            public NotePosition(int tick, int laneIndex)
            {
                Tick = tick;
                LaneIndex = laneIndex;
            }
        }
    }

    internal class ChangeShortNoteWidthOperation : EditShortNoteOperation
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
            Note.LaneIndex = AfterPosition.LaneIndex;
            Note.Width = AfterPosition.Width;
        }

        public override void Undo()
        {
            Note.LaneIndex = BeforePosition.LaneIndex;
            Note.Width = BeforePosition.Width;
        }

        public class NotePosition
        {
            public int LaneIndex { get; set; }
            public int Width { get; }

            public NotePosition(int laneIndex, int width)
            {
                LaneIndex = laneIndex;
                Width = width;
            }
        }
    }

    internal abstract class ManageShortNoteOperation<T> : IOperation
    {
        protected T Note { get; }
        protected NoteView.NoteCollection Collection { get; }
        public abstract string Description { get; }

        public ManageShortNoteOperation(NoteView.NoteCollection collection, T note)
        {
            Collection = collection;
            Note = note;
        }

        public abstract void Undo();
        public abstract void Redo();
    }

    internal class InsertTapOperation : ManageShortNoteOperation<Tap>
    {
        public override string Description { get { return "TAPの追加"; } }

        public InsertTapOperation(NoteView.NoteCollection collection, Tap note) : base(collection, note)
        {
        }

        public override void Redo()
        {
            Collection.Add(Note);
        }

        public override void Undo()
        {
            Collection.Remove(Note);
        }
    }

    internal class RemoveTapOperation : ManageShortNoteOperation<Tap>
    {
        public override string Description { get { return "TAPの削除"; } }

        public RemoveTapOperation(NoteView.NoteCollection collection, Tap note) : base(collection, note)
        {
        }

        public override void Redo()
        {
            Collection.Remove(Note);
        }

        public override void Undo()
        {
            Collection.Add(Note);
        }
    }

    internal class ChangeHoldDurationOperation : IOperation
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

    internal class ChangeHoldPositionOperation : IOperation
    {
        public string Description { get { return "HOLDの移動"; } }

        protected Hold Note { get; }
        protected NotePosition BeforePosition { get; }
        protected NotePosition AfterPosition { get; }

        public ChangeHoldPositionOperation(Hold note, NotePosition before, NotePosition after)
        {
            Note = note;
            BeforePosition = before;
            AfterPosition = after;
        }

        public void Redo()
        {
            Note.StartTick = AfterPosition.StartTick;
            Note.LaneIndex = AfterPosition.LaneIndex;
            Note.Width = AfterPosition.Width;
        }

        public void Undo()
        {
            Note.StartTick = BeforePosition.StartTick;
            Note.LaneIndex = BeforePosition.LaneIndex;
            Note.Width = BeforePosition.Width;
        }

        internal class NotePosition
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
        }
    }

    internal class MoveSlideStepNoteOperation : IOperation
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
            StepNote.LaneIndexOffset = AfterPosition.LaneIndexOffset;
        }

        public void Undo()
        {
            StepNote.TickOffset = BeforePosition.TickOffset;
            StepNote.LaneIndexOffset = BeforePosition.LaneIndexOffset;
        }

        internal class NotePosition
        {
            public int TickOffset { get; }
            public int LaneIndexOffset { get; }

            public NotePosition(int tickOffset, int laneIndexOffset)
            {
                TickOffset = tickOffset;
                LaneIndexOffset = laneIndexOffset;
            }
        }
    }

    internal class MoveSlideOperation : IOperation
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
            Note.StartLaneIndex = AfterPosition.StartLaneIndex;
            Note.Width = AfterPosition.Width;
        }

        public void Undo()
        {
            Note.StartTick = BeforePosition.StartTick;
            Note.StartLaneIndex = BeforePosition.StartLaneIndex;
            Note.Width = BeforePosition.Width;
        }

        internal class NotePosition
        {
            public int StartTick { get; }
            public int StartLaneIndex { get; }
            public int Width { get; }

            public NotePosition(int startTick, int startLaneIndex, int width)
            {
                StartTick = startTick;
                StartLaneIndex = startLaneIndex;
                Width = width;
            }
        }
    }

    internal class ChangeAirActionOffsetOperation : IOperation
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
}
