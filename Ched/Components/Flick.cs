using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Drawing;
using System.Drawing.Drawing2D;

namespace Ched.Components
{
    public class Flick : TappableBase
    {
        private static readonly Color DarkForegroundColor = Color.FromArgb(0, 96, 138);
        private static readonly Color LightForegroundColor = Color.FromArgb(122, 216, 252);
        private static readonly Color DarkBackgroundColor = Color.FromArgb(68, 68, 68);
        private static readonly Color LightBackgroundColor = Color.FromArgb(186, 186, 186);

        protected override void DrawNote(Graphics g, RectangleF rect)
        {
            using (var path = rect.ToRoundedPath(rect.Height * 0.3f))
            {
                using (var backgroundBrush = new LinearGradientBrush(rect, DarkBackgroundColor, LightBackgroundColor, LinearGradientMode.Vertical))
                {
                    g.FillPath(backgroundBrush, path);
                }
            }

            var foregroundRect = new RectangleF(rect.Left + rect.Width / 4, rect.Top, rect.Width / 2, rect.Height);
            using (var path = foregroundRect.ToRoundedPath(rect.Height * 0.3f))
            {
                using (var foregroundBrush = new LinearGradientBrush(foregroundRect, DarkForegroundColor, LightForegroundColor, LinearGradientMode.Vertical))
                {
                    g.FillPath(foregroundBrush, path);
                }
            }
            DrawTapSymbol(g, foregroundRect);
        }
    }
}