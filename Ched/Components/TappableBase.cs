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
}
