using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Drawing;
using System.Drawing.Drawing2D;

namespace Ched.Components
{
    public class Hold : MovableLongNoteBase
    {
        private static readonly Color BackgroundMiddleColor = Color.FromArgb(216, 216, 216, 0);
        private static readonly Color BackgroundEdgeColor = Color.FromArgb(216, 166, 44, 168);

        private int laneIndex;
        private int width = 1;
        private int duration;

        /// <summary>
        /// ノートの配置されるレーン番号を設定します。。
        /// </summary>
        public int LaneIndex
        {
            get { return laneIndex; }
            set
            {
                if (laneIndex == value) return;
                if (value < 0 || value >= Constants.LanesCount) throw new ArgumentOutOfRangeException("value", "Invalid lane index.");
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
                if (width == value) return;
                if (value < 1 || value + LaneIndex > Constants.LanesCount) throw new ArgumentOutOfRangeException("value", "Invalid note width.");
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
                if (duration < 0) throw new ArgumentOutOfRangeException("value", "value must not be negative.");
                duration = value;
            }
        }

        public StartTap StartNote { get; }
        public EndTap EndNote { get; }

        public Hold()
        {
            StartNote = new StartTap(this);
            EndNote = new EndTap(this);
        }

        internal void DrawBackground(Graphics g, RectangleF rect)
        {
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
        }
        
        public override int GetDuration()
        {
            return Duration;
        }

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
