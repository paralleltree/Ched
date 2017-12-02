using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Drawing;
using System.Drawing.Drawing2D;

namespace Ched.Components
{
    public class Slide : LongNoteBase
    {
        private static readonly Color BackgroundMiddleColor = Color.FromArgb(216, 0, 164, 146);
        private static readonly Color BackgroundEdgeColor = Color.FromArgb(216, 166, 44, 168);
        private static readonly Color BackgroundLineColor = Color.FromArgb(216, 0, 214, 192);

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
                using (var pen = new Pen(BackgroundLineColor))
                {
                    g.DrawLine(pen, rect.Left + rect.Width / 2, rect.Top, rect.Left + rect.Width / 2, rect.Bottom);
                }
            }
        }

        public override int GetDuration()
        {
            throw new NotImplementedException();
        }

        //public class Tap : LongNoteTapBase
        //{
        //    private readonly Color DarkNoteColor = Color.FromArgb(0, 16, 138);
        //    private readonly Color LightNoteColor = Color.FromArgb(86, 106, 255);

        //    protected override void DrawNote(Graphics g, RectangleF rect)
        //    {
        //        DrawNote(g, rect, DarkNoteColor, LightNoteColor);
        //    }
        //}
    }
}
