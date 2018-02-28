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
    public class Slide : MovableLongNoteBase
    {
        private static readonly Color BackgroundMiddleColor = Color.FromArgb(196, 0, 164, 146);
        private static readonly Color BackgroundEdgeColor = Color.FromArgb(196, 166, 44, 168);
        private static readonly Color BackgroundLineColor = Color.FromArgb(196, 0, 214, 192);


        [Newtonsoft.Json.JsonProperty]
        private int width = 1;
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
                if (StepNotes.Any(p =>
                {
                    int laneIndex = value + p.LaneIndexOffset;
                    return laneIndex < 0 || laneIndex + Width > Constants.LanesCount;
                })) throw new ArgumentOutOfRangeException("value", "Invalid lane index.");
                if (startLaneIndex < 0 || startLaneIndex + Width > Constants.LanesCount) throw new ArgumentOutOfRangeException("value", "Invalid lane index.");
                startLaneIndex = value;
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
                if (value < 1 || StartLaneIndex + (StepNotes.Count > 0 ? StepNotes.Max(p => p.LaneIndexOffset) : 0) + value > Constants.LanesCount) throw new ArgumentOutOfRangeException("value", "Invalid note width.");
                width = value;
            }
        }

        public List<StepTap> StepNotes { get { return stepNotes; } }
        public StartTap StartNote { get; }

        public Slide()
        {
            StartNote = new StartTap(this);
        }

        /// <summary>
        /// SLIDEの背景を描画します。
        /// </summary>
        /// <param name="g">描画先Graphics</param>
        /// <param name="width1">ノートの描画幅</param>
        /// <param name="x1">開始ノートの左端位置</param>
        /// <param name="y1">開始ノートのY座標</param>
        /// <param name="x2">終了ノートの左端位置</param>
        /// <param name="y2">終了ノートのY座標</param>
        /// <param name="gradStartY">始点Step以前の中継点のY座標(グラデーション描画用)</param>
        /// <param name="gradEndY">終点Step以後の中継点のY座標(グラデーション描画用)</param>
        /// <param name="noteHeight">ノートの描画高さ</param>
        internal void DrawBackground(Graphics g, float width1, float width2, float x1, float y1, float x2, float y2, float gradStartY, float gradEndY, float noteHeight)
        {
            var prevMode = g.SmoothingMode;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            var rect = new RectangleF(Math.Min(x1, x2), Math.Min(y1, y2), Math.Abs(x1 - x2) + width1, Math.Abs(y1 - y2));
            var gradientRect = new RectangleF(rect.Left, gradStartY, rect.Width, gradEndY - gradStartY);
            using (var brush = new LinearGradientBrush(gradientRect, BackgroundEdgeColor, BackgroundMiddleColor, LinearGradientMode.Vertical))
            {
                var blend = new ColorBlend(4)
                {
                    Colors = new Color[] { BackgroundEdgeColor, BackgroundMiddleColor, BackgroundMiddleColor, BackgroundEdgeColor },
                    Positions = new float[] { 0.0f, 0.3f, 0.7f, 1.0f }
                };
                brush.InterpolationColors = blend;
                using (var path = GetBackgroundPath(width1, width2, x1, y1, x2, y2))
                {
                    g.FillPath(brush, path);
                }
            }
            using (var pen = new Pen(BackgroundLineColor, noteHeight * 0.4f))
            {
                g.DrawLine(pen, x1 + width1 / 2, y1, x2 + width2 / 2, y2);
            }
            g.SmoothingMode = prevMode;
        }

        internal GraphicsPath GetBackgroundPath(float width1, float width2, float x1, float y1, float x2, float y2)
        {
            var path = new GraphicsPath();
            path.AddPolygon(new PointF[]
            {
                new PointF(x1, y1),
                new PointF(x1 + width1, y1),
                new PointF(x2 + width2, y2),
                new PointF(x2, y2)
            });
            return path;
        }

        public override int GetDuration()
        {
            return StepNotes.Max(p => p.TickOffset);
        }

        [Newtonsoft.Json.JsonObject(Newtonsoft.Json.MemberSerialization.OptIn)]
        public abstract class TapBase : LongNoteTapBase
        {
            private readonly Color DarkNoteColor = Color.FromArgb(0, 16, 138);
            private readonly Color LightNoteColor = Color.FromArgb(86, 106, 255);

            [Newtonsoft.Json.JsonProperty]
            private Slide parentNote;

            public Slide ParentNote { get { return parentNote; } }

            public override int Width { get { return ParentNote.Width; } }

            public TapBase(Slide parent)
            {
                parentNote = parent;
            }

            protected override void DrawNote(Graphics g, RectangleF rect)
            {
                DrawNote(g, rect, DarkNoteColor, LightNoteColor);
            }
        }

        public class StartTap : TapBase
        {
            public override bool IsTap { get { return true; } }

            public override int Tick { get { return ParentNote.StartTick; } }

            public override int LaneIndex { get { return ParentNote.StartLaneIndex; } }

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
                    if (laneIndexOffset == value) return;
                    int laneIndex = ParentNote.StartNote.LaneIndex + laneIndexOffset;
                    if (laneIndex < 0 || laneIndex + Width > Constants.LanesCount) throw new ArgumentOutOfRangeException("value", "Invalid lane index offset.");
                    laneIndexOffset = value;
                }
            }

            public StepTap(Slide parent) : base(parent)
            {
            }

            protected override void DrawNote(Graphics g, RectangleF rect)
            {
                if (!IsVisible) return;
                base.DrawNote(g, rect);
            }
        }
    }
}
