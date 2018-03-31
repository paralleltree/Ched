using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Drawing;
using System.Drawing.Drawing2D;

namespace Ched.Components.Notes
{
    [Newtonsoft.Json.JsonObject(Newtonsoft.Json.MemberSerialization.OptIn)]
    public class Hold : MovableLongNoteBase
    {
        private static readonly Color BackgroundMiddleColor = Color.FromArgb(196, 216, 216, 0);
        private static readonly Color BackgroundEdgeColor = Color.FromArgb(196, 166, 44, 168);

        [Newtonsoft.Json.JsonProperty]
        private int laneIndex;
        [Newtonsoft.Json.JsonProperty]
        private int width = 1;
        [Newtonsoft.Json.JsonProperty]
        private int duration = 1;

        [Newtonsoft.Json.JsonProperty]
        private StartTap startNote;
        [Newtonsoft.Json.JsonProperty]
        private EndTap endNote;

        /// <summary>
        /// ノートの配置されるレーン番号を設定します。。
        /// </summary>
        public int LaneIndex
        {
            get { return laneIndex; }
            set
            {
                CheckPosition(value, Width);
                laneIndex = value;
            }
        }

        /// <summary>
        /// ノートのレーン幅を設定します。
        /// </summary>
        public int Width
        {
            get { return width; }
            set
            {
                CheckPosition(LaneIndex, value);
                width = value;
            }
        }

        /// <summary>
        /// ノートの長さを設定します。
        /// </summary>
        public int Duration
        {
            get { return duration; }
            set
            {
                if (duration == value) return;
                if (duration <= 0) throw new ArgumentOutOfRangeException("value", "value must be positive.");
                duration = value;
            }
        }

        protected void CheckPosition(int laneIndex, int width)
        {
            if (width < 1 || width > Constants.LanesCount)
                throw new ArgumentOutOfRangeException("width", "Invalid width.");
            if (laneIndex < 0 || laneIndex + width > Constants.LanesCount)
                throw new ArgumentOutOfRangeException("laneIndex", "Invalid lane index.");
        }

        public void SetPosition(int laneIndex, int width)
        {
            CheckPosition(laneIndex, width);
            this.laneIndex = laneIndex;
            this.width = width;
        }

        public StartTap StartNote { get { return startNote; } }
        public EndTap EndNote { get { return endNote; } }

        public Hold()
        {
            startNote = new StartTap(this);
            endNote = new EndTap(this);
        }

        internal void DrawBackground(Graphics g, RectangleF rect)
        {
            var prevMode = g.SmoothingMode;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            using (var brush = new LinearGradientBrush(rect, BackgroundEdgeColor, BackgroundMiddleColor, LinearGradientMode.Vertical))
            {
                var blend = new ColorBlend(4)
                {
                    Colors = new Color[] { BackgroundEdgeColor, BackgroundMiddleColor, BackgroundMiddleColor, BackgroundEdgeColor },
                    Positions = new float[] { 0.0f, 0.3f, 0.7f, 1.0f }
                };
                brush.InterpolationColors = blend;
                g.FillRectangle(brush, rect);
            }
            g.SmoothingMode = prevMode;
        }

        public override int GetDuration()
        {
            return Duration;
        }

        [Newtonsoft.Json.JsonObject(Newtonsoft.Json.MemberSerialization.OptIn)]
        public abstract class TapBase : LongNoteTapBase
        {
            private static readonly Color DarkNoteColor = Color.FromArgb(196, 86, 0);
            private static readonly Color LightNoteColor = Color.FromArgb(244, 156, 102);

            protected Hold parent;

            public override int LaneIndex { get { return parent.LaneIndex; } }

            public override int Width { get { return parent.Width; } }

            public TapBase(Hold parent)
            {
                this.parent = parent;
            }

            protected override void DrawNote(Graphics g, RectangleF rect)
            {
                DrawNote(g, rect, DarkNoteColor, LightNoteColor);
            }
        }

        public class StartTap : TapBase
        {
            public override int Tick { get { return parent.StartTick; } }

            public override bool IsTap { get { return true; } }

            public StartTap(Hold parent) : base(parent)
            {
            }
        }

        public class EndTap : TapBase
        {
            public override bool IsTap { get { return false; } }

            public override int Tick { get { return parent.StartTick + parent.Duration; } }

            public EndTap(Hold parent) : base(parent)
            {
            }
        }
    }
}
