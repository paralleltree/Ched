using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Drawing;
using System.Drawing.Drawing2D;

namespace Ched.Components
{
    public class Slide : LongNoteBase
    {
        private static readonly Color BackgroundMiddleColor = Color.FromArgb(216, 0, 164, 146);
        private static readonly Color BackgroundEdgeColor = Color.FromArgb(216, 166, 44, 168);
        private static readonly Color BackgroundLineColor = Color.FromArgb(216, 0, 214, 192);

        private int width = 1;

        /// <summary>
        /// ノートのレーン幅を設定します。
        /// </summary>
        public int Width
        {
            get { return width; }
            set
            {
                if (width == value) return;
                if (value < 1 || value > Constants.LanesCount) throw new ArgumentOutOfRangeException("value", "Invalid note width.");
                width = value;
            }
        }

        public List<StepTap> StepNotes { get; } = new List<StepTap>();
        public StartTap StartNote { get; }

        public Slide()
        {
            StartNote = new StartTap(this);
        }

        /// <summary>
        /// SLIDEの背景を描画します。
        /// </summary>
        /// <param name="g">描画先Graphics</param>
        /// <param name="width">ノートの描画幅</param>
        /// <param name="x1">開始ノートの左端位置</param>
        /// <param name="y1">開始ノートのY座標</param>
        /// <param name="x2">終了ノートの左端位置</param>
        /// <param name="y2">終了ノートのY座標</param>
        internal void DrawBackground(Graphics g, float width, float x1, float y1, float x2, float y2, float noteHeight)
        {
            var rect = new RectangleF(Math.Min(x1, x2), Math.Min(y1, y2), Math.Abs(x1 - x2), Math.Abs(y1 - y2));
            using (var brush = new LinearGradientBrush(rect, BackgroundEdgeColor, BackgroundMiddleColor, LinearGradientMode.Vertical))
            {
                var blend = new ColorBlend(4)
                {
                    Colors = new Color[] { BackgroundEdgeColor, BackgroundMiddleColor, BackgroundMiddleColor, BackgroundEdgeColor },
                    Positions = new float[] { 0.0f, 0.3f, 0.7f, 1.0f }
                };
                brush.InterpolationColors = blend;
                using (var path = new GraphicsPath())
                {
                    path.AddPolygon(new PointF[]
                    {
                        new PointF(x1, y1),
                        new PointF(x1 + width, y1),
                        new PointF(x2 + width, y2),
                        new PointF(x2, y2)
                    });
                    g.FillPath(brush, path);
                }
            }
            using (var pen = new Pen(BackgroundLineColor, noteHeight * 0.4f))
            {
                g.DrawLine(pen, x1 + width / 2, y1, x2 + width / 2, y2);
            }
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
                using (var pen = new Pen(BackgroundLineColor))
                {
                    g.DrawLine(pen, rect.Left + rect.Width / 2, rect.Top, rect.Left + rect.Width / 2, rect.Bottom);
                }
            }
        }

        public override int GetDuration()
        {
            return StepNotes.Max(p => p.Offset);
        }

        public abstract class TapBase : MovableLongNoteTapBase
        {
            private readonly Color DarkNoteColor = Color.FromArgb(0, 16, 138);
            private readonly Color LightNoteColor = Color.FromArgb(86, 106, 255);

            protected Slide parent;
            private int laneIndex;

            public override int Width { get { return parent.Width; } }

            public override int LaneIndex
            {
                get { return laneIndex; }
                set
                {
                    if (laneIndex == value) return;
                    if (laneIndex < 0 || laneIndex + Width > 16) throw new ArgumentOutOfRangeException("value", "Invalid lane index.");
                    laneIndex = value;
                }
            }

            public TapBase(Slide parent)
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
            public override bool IsTap { get; set; } = true;

            public override int Tick { get { return parent.StartTick; } }

            public StartTap(Slide parent) : base(parent)
            {
            }
        }

        public class StepTap : TapBase
        {
            public int Offset { get; set; }

            public override bool IsTap { get; set; }

            public override int Tick { get { return parent.StartTick + Offset; } }

            public StepTap(Slide parent) : base(parent)
            {
            }
        }
    }
}
