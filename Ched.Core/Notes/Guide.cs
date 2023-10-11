using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ched.Core.Notes
{
    [Newtonsoft.Json.JsonObject(Newtonsoft.Json.MemberSerialization.OptIn)]
    public class Guide : MovableLongNoteBase
    {
        [Newtonsoft.Json.JsonProperty]
        private float startWidth = 1;
        [Newtonsoft.Json.JsonProperty]
        private float startLaneIndex;
        [Newtonsoft.Json.JsonProperty]
        private int channel;
        [Newtonsoft.Json.JsonProperty]
        private List<StepTap> stepNotes = new List<StepTap>();

        private Constants constants = new Constants();

        /// <summary>
        /// 開始ノートの配置されるレーン番号を設定します。。
        /// </summary>
        public float StartLaneIndex
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
        public float StartWidth
        {
            get { return startWidth; }
            set
            {
                CheckPosition(startLaneIndex, value);
                startWidth = value;
            }
        }

        /// <summary>
        /// チャンネルを設定します。
        /// </summary>
        public int Channel
        {
            get { return channel; }
            set
            {
                channel = value;
            }
        }

        public List<StepTap> StepNotes { get { return stepNotes; } }
        public StartTap StartNote { get; }

        public Guide()
        {
            StartNote = new StartTap(this);
        }

        protected void CheckPosition(float startLaneIndex, float startWidthth)
        {
            float maxRightOffset = Math.Max(0, StepNotes.Count == 0 ? 0 : StepNotes.Max(p => p.LaneIndexOffset + p.WidthChange));
            /*
            if (startWidth < Math.Abs(Math.Min(0, StepNotes.Count == 0 ? 0 : StepNotes.Min(p => p.WidthChange))) + 0.1 || startLaneIndex + startWidth + maxRightOffset > Constants.LanesCount)
                throw new ArgumentOutOfRangeException("startWidth", "Invalid note width.");
                
            if (StepNotes.Any(p =>
            {
                float laneIndex = startLaneIndex + p.LaneIndexOffset;
                return laneIndex < constants.MinusLaneCount -8 || laneIndex + (startWidth + p.WidthChange) > constants.LaneCount + 8;
            })) throw new ArgumentOutOfRangeException("startLaneIndex", "Invalid lane index.");
            if (startLaneIndex < constants.MinusLaneCount || startLaneIndex + startWidth > constants.LaneCount)
                throw new ArgumentOutOfRangeException("startLaneIndex", "Invalid lane index.");
            */
        }

        public void SetPosition(float startLaneIndex, float startWidth)
        {
            CheckPosition(startLaneIndex, startWidth);
            this.startLaneIndex = startLaneIndex;
            this.startWidth = startWidth;
        }

        public void SetChannel(int Channel)
        {
            this.channel = Channel;
        }

        /// <summary>
        /// このガイドを反転します。
        /// </summary>
        public void Flip()
        {
            startLaneIndex =  Constants.LanesCount - startLaneIndex - startWidth;
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
        public abstract class TapBase : LongNoteTapBase, IAirable
        {
            [Newtonsoft.Json.JsonProperty]
            private Guide parentNote;

            public Guide ParentNote { get { return parentNote; } }

            public TapBase(Guide parent)
            {
                parentNote = parent;
            }
        }

        public class StartTap : TapBase, IAirable
        {
            public override bool IsTap { get { return true; } }

            public override int Tick { get { return ParentNote.StartTick; } }

            public override float LaneIndex { get { return ParentNote.StartLaneIndex; } }

            public override float Width { get { return ParentNote.StartWidth; } }

            public override int Channel { get { return ParentNote.Channel; } set { ParentNote.Channel = value; } }

            public StartTap(Guide parent) : base(parent)
            {
            }
        }

        [Newtonsoft.Json.JsonObject(Newtonsoft.Json.MemberSerialization.OptIn)]
        public class StepTap : TapBase, IAirable
        {
            [Newtonsoft.Json.JsonProperty]
            private float laneIndexOffset;
            [Newtonsoft.Json.JsonProperty]
            private float widthChange;
            [Newtonsoft.Json.JsonProperty]
            private int tickOffset = 1;
            [Newtonsoft.Json.JsonProperty]
            private int channel;
            [Newtonsoft.Json.JsonProperty]
            private bool isVisible = true;

            private Constants constants = new Constants();

            public int TickOffset
            {
                get { return tickOffset; }
                set
                {
                    //if (value <= 0) throw new ArgumentOutOfRangeException("value", "value must be positive.");
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

            public override float LaneIndex { get { return ParentNote.StartLaneIndex + LaneIndexOffset; } }

            public override int Channel
            {
                get { return channel; }
                set { channel = value; }
            }

            public float LaneIndexOffset
            {
                get { return laneIndexOffset; }
                set
                {
                    CheckPosition(value, widthChange);
                    laneIndexOffset = value;
                }
            }

            public float WidthChange
            {
                get { return widthChange; }
                set
                {
                    CheckPosition(laneIndexOffset, value);
                    widthChange = value;
                }
            }

            public override float Width { get { return ParentNote.StartWidth + WidthChange; } }

            public StepTap(Guide parent) : base(parent)
            {
            }

            public void SetPosition(float laneIndexOffset, float widthChange)
            {
                CheckPosition(laneIndexOffset, widthChange);
                this.laneIndexOffset = laneIndexOffset;
                this.widthChange = widthChange;
            }


            protected void CheckPosition(float laneIndexOffset, float widthChange)
            {
                float laneIndex = ParentNote.StartNote.LaneIndex + laneIndexOffset;
                //if (laneIndex < constants.MinusLaneCount || laneIndex + (ParentNote.StartWidth + widthChange) > constants.LaneCount)
                    //throw new ArgumentOutOfRangeException("laneIndexOffset", "Invalid lane index offset.");

                float actualWidth = widthChange + ParentNote.StartWidth;
                if (actualWidth < 0.01 )
                    throw new ArgumentOutOfRangeException("widthChange", "Invalid width change value.");
            }
        }
    }
}
