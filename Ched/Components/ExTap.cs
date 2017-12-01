using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Drawing;
using System.Drawing.Drawing2D;

namespace Ched.Components
{
    public class ExTap : TappableBase
    {
        private static readonly Color DarkNoteColor = Color.FromArgb(204, 192, 0);
        private static readonly Color LightNoteColor = Color.FromArgb(255, 236, 68);

        protected override void DrawNote(Graphics g, RectangleF rect)
        {
            using (var path = rect.ToRoundedPath(rect.Height * 0.3f))
            {
                using (var brush = new LinearGradientBrush(rect, LightNoteColor, DarkNoteColor, LinearGradientMode.Vertical))
                {
                    g.FillPath(brush, path);
                }
            }
            DrawTapSymbol(g, rect);
        }
    }
}
