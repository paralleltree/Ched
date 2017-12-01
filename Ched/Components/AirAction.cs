using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Drawing;
using System.Drawing.Drawing2D;

namespace Ched.Components
{
    public class AirAction : LongNoteBase
    {
        private static readonly Color LineColor = Color.FromArgb(216, 0, 146, 0);

        internal void Draw(Graphics g, RectangleF targetNoteRect)
        {
        }

        internal class ActionNote : ShortNoteBase
        {
            private readonly Color LightNoteColor = Color.FromArgb(212, 92, 255);
            private readonly Color DarkNoteColor = Color.FromArgb(146, 0, 192);

            internal void Draw(Graphics g, RectangleF rect)
            {
                using (var brush = new LinearGradientBrush(rect, LightNoteColor, DarkNoteColor, LinearGradientMode.Vertical))
                {
                    g.FillRectangle(brush, rect);
                }
                using (var brush = new LinearGradientBrush(rect.Expand(rect.Height * 0.1f), LightBorderColor, DarkBorderColor, LinearGradientMode.Vertical))
                {
                    using (var pen = new Pen(brush, rect.Height * 0.1f))
                    {
                        g.DrawRectangle(pen, rect.X, rect.Y, rect.Width, rect.Height);
                    }
                }
            }
        }
    }
}
