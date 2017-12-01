using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Drawing;
using System.Drawing.Drawing2D;

namespace Ched.Components
{
    public class Hold : LongNoteBase
    {
        private static readonly Color BackgroundMiddleColor = Color.FromArgb(216, 216, 216, 0);
        private static readonly Color BackgroundEdgeColor = Color.FromArgb(216, 166, 44, 168);

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
            }
        }

        internal class Tap : LongNoteTapBase
        {
            private readonly Color DarkNoteColor = Color.FromArgb(196, 86, 0);
            private readonly Color LightNoteColor = Color.FromArgb(244, 156, 102);

            protected override void DrawNote(Graphics g, RectangleF rect)
            {
                using (var path = rect.ToRoundedPath(rect.Height * 0.3f))
                {
                    using (var brush = new LinearGradientBrush(rect, LightNoteColor, DarkNoteColor, LinearGradientMode.Vertical))
                    {
                        g.FillPath(brush, path);
                    }
                }
            }
        }
    }
}
