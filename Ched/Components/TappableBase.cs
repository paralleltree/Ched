using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Drawing;
using System.Drawing.Drawing2D;

namespace Ched.Components
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

    public abstract class TappableBase : TapBase, IAirable
    {
        private int tick;
        private int laneIndex;
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
                if (laneIndex == value) return;
                if (value < 0 || value + Width > Constants.LanesCount) throw new ArgumentOutOfRangeException("value", "Invalid lane index.");
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
    }

    public static class GraphicsExtensions
    {
        // ref: http://csharphelper.com/blog/2016/01/draw-rounded-rectangles-in-c/
        public static GraphicsPath ToRoundedPath(this RectangleF rect, float radius)
        {
            if (radius * 2 > Math.Min(rect.Width, rect.Height))
            {
                throw new ArgumentException("radius must be less than short side.", "radius");
            }

            var path = new GraphicsPath();

            path.AddArc(rect.Left, rect.Top, radius * 2, radius * 2, 180, 90);
            //path.AddLine(rect.Left + radius, rect.Top, rect.Right - radius, rect.Top);
            path.AddArc(rect.Right - radius * 2, rect.Top, radius * 2, radius * 2, 270, 90);
            //path.AddLine(rect.Right, rect.Top + radius, rect.Right, rect.Bottom - radius);
            path.AddArc(rect.Right - radius * 2, rect.Bottom - radius * 2, radius * 2, radius * 2, 0, 90);
            //path.AddLine(rect.Right - radius, rect.Bottom, rect.Left + radius, rect.Bottom);
            path.AddArc(rect.Left, rect.Bottom - radius * 2, radius * 2, radius * 2, 90, 90);
            //path.AddLine(rect.Left, rect.Bottom - radius, rect.Left, rect.Top + radius);
            path.CloseFigure();
            return path;
        }

        public static RectangleF Expand(this RectangleF rect, float size)
        {
            return new RectangleF(rect.Left - size, rect.Top - size, rect.Width + size * 2, rect.Height + size * 2);
        }
    }
}
