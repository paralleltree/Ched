using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Drawing;
using System.Drawing.Drawing2D;

namespace Ched.Components.Notes
{
    public abstract class TapBase : ShortNoteBase
    {
        internal virtual void Draw(Graphics g, RectangleF rect)
        {
            DrawNote(g, rect);
            DrawBorder(g, rect);
        }

        protected virtual void DrawBorder(Graphics g, RectangleF rect)
        {
            float borderWidth = rect.Height * 0.1f;
            using (var brush = new LinearGradientBrush(rect.Expand(borderWidth), DarkBorderColor, LightBorderColor, LinearGradientMode.Vertical))
            {
                using (var pen = new Pen(brush, borderWidth))
                {
                    using (var path = rect.ToRoundedPath(rect.Height * 0.3f))
                    {
                        g.DrawPath(pen, path);
                    }
                }
            }
        }

        protected abstract void DrawNote(Graphics g, RectangleF rect);

        protected void DrawNote(Graphics g, RectangleF rect, Color darkColor, Color lightColor)
        {
            using (var path = rect.ToRoundedPath(rect.Height * 0.3f))
            {
                using (var brush = new LinearGradientBrush(rect, darkColor, lightColor, LinearGradientMode.Vertical))
                {
                    g.FillPath(brush, path);
                }
            }
        }

        protected void DrawTapSymbol(Graphics g, RectangleF noteRect)
        {
            using (var pen = new Pen(Color.White, noteRect.Height * 0.1f))
            {
                g.DrawLine(pen, noteRect.Left + noteRect.Width * 0.2f, noteRect.Top + noteRect.Height / 2f, noteRect.Right - noteRect.Width * 0.2f, noteRect.Top + noteRect.Height / 2);
            }
        }
    }

    [Newtonsoft.Json.JsonObject(Newtonsoft.Json.MemberSerialization.OptIn)]
    public abstract class TappableBase : TapBase, IAirable
    {
        [Newtonsoft.Json.JsonProperty]
        private int tick;
        [Newtonsoft.Json.JsonProperty]
        private int laneIndex;
        [Newtonsoft.Json.JsonProperty]
        private int width = 1;

        /// <summary>
        /// ノートの位置を表すTickを設定します。
        /// </summary>
        public int Tick
        {
            get { return tick; }
            set
            {
                if (tick == value) return;
                if (tick < 0) throw new ArgumentOutOfRangeException("value", "value must not be negative.");
                tick = value;
            }
        }

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
    }
}
