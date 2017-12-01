using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Drawing;
using System.Drawing.Drawing2D;

namespace Ched.Components
{
    public class Damage : TappableBase
    {
        private static readonly Color ForegroundDarkColor = Color.FromArgb(8, 8, 116);
        private static readonly Color ForegroundLightColor = Color.FromArgb(22, 40, 180);

        protected override void DrawBorder(Graphics g, RectangleF rect)
        {
            float borderWidth = rect.Height * 0.1f;
            using (var brush = new LinearGradientBrush(rect.Expand(borderWidth), LightBorderColor, DarkBorderColor, LinearGradientMode.Vertical))
            {
                using (var pen = new Pen(brush, borderWidth))
                {
                    g.DrawRectangle(pen, rect.X, rect.Y, rect.Width, rect.Height);
                }
            }
        }

        protected override void DrawNote(Graphics g, RectangleF rect)
        {
            using (var brush = new LinearGradientBrush(rect, ForegroundLightColor, ForegroundDarkColor, LinearGradientMode.Vertical))
            {
                g.FillRectangle(brush, rect.X, rect.Y, rect.Width, rect.Height);
            }
        }
    }
}
