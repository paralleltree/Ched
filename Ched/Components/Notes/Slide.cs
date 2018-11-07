using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ched.Components.Notes
{
    [Newtonsoft.Json.JsonObject(Newtonsoft.Json.MemberSerialization.OptIn)]
    public class Slide : MovableLongNoteBase
    {
        [Newtonsoft.Json.JsonProperty]
        private int startWidth = 1;
        [Newtonsoft.Json.JsonProperty]
        private int startLaneIndex;
        [Newtonsoft.Json.JsonProperty]
        private List<StepTap> stepNotes = new List<StepTap>();

        /// <summary>
        /// 開始ノートの配置されるレーン番号を設定します。。
        /// </summary>
        public int StartLaneIndex
        {
            get { return startLaneIndex; }
            set
            {
                CheckPosition(value, startWidth);
                startLaneIndex = value;
            }
        }

        /// <summary>
        /// 開始ノートのレーン幅を設定します。
        /// </summary>
        public int StartWidth
        {
            get { return startWidth; }
            set
            {
                CheckPosition(startLaneIndex, value);
                startWidth = value;
            }
        }

        public List<StepTap> StepNotes { get { return stepNotes; } }
        public StartTap StartNote { get; }

        public Slide()
        {
            StartNote = new StartTap(this);
        }

        protected void CheckPosition(int startLaneIndex, int startWidth)
        {
            int maxRightOffset = Math.Max(0, StepNotes.Count == 0 ? 0 : StepNotes.Max(p => p.LaneIndexOffset + p.WidthChange));
            if (startWidth < Math.Abs(Math.Min(0, StepNotes.Count == 0 ? 0 : StepNotes.Min(p => p.WidthChange))) + 1 || startLaneIndex + startWidth + maxRightOffset > Constants.LanesCount)
                throw new ArgumentOutOfRangeException("startWidth", "Invalid note width.");

            if (StepNotes.Any(p =>
            {
                int laneIndex = startLaneIndex + p.LaneIndexOffset;
                return laneIndex < 0 || laneIndex + (startWidth + p.WidthChange) > Constants.LanesCount;
            })) throw new ArgumentOutOfRangeException("startLaneIndex", "Invalid lane index.");
            if (startLaneIndex < 0 || startLaneIndex + startWidth > Constants.LanesCount)
                throw new ArgumentOutOfRangeException("startLaneIndex", "Invalid lane index.");
        }

        public void SetPosition(int startLaneIndex, int startWidth)
        {
            CheckPosition(startLaneIndex, startWidth);
            this.startLaneIndex = startLaneIndex;
            this.startWidth = startWidth;
        }

        /// <summary>
        /// このスライドを反転します。
        /// </summary>
        public void Flip()
        {
            startLaneIndex = Constants.LanesCount - startLaneIndex - startWidth;
            foreach (var step in StepNotes)
            {
                step.LaneIndexOffset = -step.LaneIndexOffset - step.WidthChange;
            }
        }

        public override int GetDuration()
        {
            return StepNotes.Max(p => p.TickOffset);
        }

        [Newtonsoft.Json.JsonObject(Newtonsoft.Json.MemberSerialization.OptIn)]
        public abstract class TapBase : LongNoteTapBase
        {
            [Newtonsoft.Json.JsonProperty]
            private Slide parentNote;

            public Slide ParentNote { get { return parentNote; } }

            public TapBase(Slide parent)
            {
                parentNote = parent;
            }
        }

        public class StartTap : TapBase
        {
            public override bool IsTap { get { return true; } }

            public override int Tick { get { return ParentNote.StartTick; } }

            public override int LaneIndex { get { return ParentNote.StartLaneIndex; } }

            public override int Width { get { return ParentNote.StartWidth; } }

            public StartTap(Slide parent) : base(parent)
            {
            }
        }

        [Newtonsoft.Json.JsonObject(Newtonsoft.Json.MemberSerialization.OptIn)]
        public class StepTap : TapBase
        {
            [Newtonsoft.Json.JsonProperty]
            private int laneIndexOffset;
            [Newtonsoft.Json.JsonProperty]
            private int widthChange;
            [Newtonsoft.Json.JsonProperty]
            private int tickOffset = 1;
            [Newtonsoft.Json.JsonProperty]
            private bool isVisible = true;

            public int TickOffset
            {
                get { return tickOffset; }
                set
                {
                    if (value <= 0) throw new ArgumentOutOfRangeException("value", "value must be positive.");
                    tickOffset = value;
                }
            }


            public bool IsVisible
            {
                get { return isVisible; }
                set { isVisible = value; }
            }

            public override bool IsTap { get { return false; } }

            public override int Tick { get { return ParentNote.StartTick + TickOffset; } }

            public override int LaneIndex { get { return ParentNote.StartLaneIndex + LaneIndexOffset; } }

            public int LaneIndexOffset
            {
                get { return laneIndexOffset; }
                set
                {
                    CheckPosition(value, widthChange);
                    laneIndexOffset = value;
                }
            }

            public int WidthChange
            {
                get { return widthChange; }
                set
                {
                    CheckPosition(laneIndexOffset, value);
                    widthChange = value;
                }
            }

            public override int Width { get { return ParentNote.StartWidth + WidthChange; } }

            public StepTap(Slide parent) : base(parent)
            {
            }

            public void SetPosition(int laneIndexOffset, int widthChange)
            {
                CheckPosition(laneIndexOffset, widthChange);
                this.laneIndexOffset = laneIndexOffset;
                this.widthChange = widthChange;
            }

            protected void CheckPosition(int laneIndexOffset, int widthChange)
            {
                int laneIndex = ParentNote.StartNote.LaneIndex + laneIndexOffset;
                if (laneIndex < 0 || laneIndex + (ParentNote.StartWidth + widthChange) > Constants.LanesCount)
                    throw new ArgumentOutOfRangeException("laneIndexOffset", "Invalid lane index offset.");

                int actualWidth = widthChange + ParentNote.StartWidth;
                if (actualWidth < 1 || laneIndex + actualWidth > Constants.LanesCount)
                    throw new ArgumentOutOfRangeException("widthChange", "Invalid width change value.");
            }
        }
    }
}
