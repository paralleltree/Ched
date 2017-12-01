using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Drawing;
using System.Drawing.Drawing2D;

namespace Ched.Components
{
    public abstract class TappableBase : ShortNoteBase
    {
        internal virtual void Draw(Graphics g, RectangleF rect)
        {
            DrawNote(g, rect);
            DrawBorder(g, rect);
        }

        protected virtual void DrawBorder(Graphics g, RectangleF rect)
        {
            float borderWidth = rect.Height * 0.1f;
            using (var brush = new LinearGradientBrush(rect.Expand(borderWidth), LightBorderColor, DarkBorderColor, LinearGradientMode.Vertical))
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

        protected void DrawTapSymbol(Graphics g, RectangleF noteRect)
        {
            using (var pen = new Pen(Color.White, noteRect.Height * 0.1f))
            {
                g.DrawLine(pen, noteRect.Left + noteRect.Width * 0.2f, noteRect.Top + noteRect.Height / 2f, noteRect.Right - noteRect.Width * 0.2f, noteRect.Top + noteRect.Height / 2);
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
