using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ched.Drawing
{
    internal static class ComponentGraphics
    {
        public static void DrawTappableNote(this Graphics g, RectangleF rect, GradientColor foregroundColors, GradientColor borderColors)
        {
            g.DrawNote(rect, foregroundColors, borderColors);
            g.DrawTapSymbol(rect, 1);
        }

        public static void DrawNote(this Graphics g, RectangleF rect, GradientColor foregroundColors, GradientColor borderColors)
        {
            DrawNoteBase(g, rect, foregroundColors);
            DrawBorder(g, rect, borderColors);
        }

        public static void DrawNoteBase(this Graphics g, RectangleF rect, GradientColor colors)
        {
            using (var path = rect.ToRoundedPath(rect.Height * 0.3f))
            {
                using (var brush = new LinearGradientBrush(rect, colors.DarkColor, colors.LightColor, LinearGradientMode.Vertical))
                {
                    g.FillPath(brush, path);
                }
            }
        }

        public static void DrawBorder(this Graphics g, RectangleF rect, GradientColor colors)
        {
            float borderWidth = rect.Height * 0.1f;
            using (var brush = new LinearGradientBrush(rect.Expand(borderWidth), colors.DarkColor, colors.LightColor, LinearGradientMode.Vertical))
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

        public static void DrawBorder(this Graphics g, RectangleF rect, GradientColor colors, float width)
        {
            float borderWidth = rect.Height * width;
            using (var brush = new LinearGradientBrush(rect.Expand(borderWidth), colors.DarkColor, colors.LightColor, LinearGradientMode.Vertical))
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

        public static void DrawSquarishNote(this Graphics g, RectangleF rect, GradientColor foregroundColors, GradientColor borderColors)
        {
            float borderWidth = rect.Height * 0.1f;
            using (var brush = new LinearGradientBrush(rect, foregroundColors.DarkColor, foregroundColors.LightColor, LinearGradientMode.Vertical))
            {
                g.FillRectangle(brush, rect);
            }

            using (var brush = new LinearGradientBrush(rect.Expand(borderWidth), borderColors.DarkColor, borderColors.LightColor, LinearGradientMode.Vertical))
            {
                using (var pen = new Pen(brush, borderWidth))
                {
                    g.DrawRectangle(pen, rect.X, rect.Y, rect.Width, rect.Height);
                }
            }
        }

        public static void DrawTapSymbol(this Graphics g, RectangleF rect, int mode)
        {

            using (var pen = new Pen(Color.White, rect.Height * 0.1f))
            {
                switch (mode)
                {
                    case 0:
                        break;
                    case 1:
                        g.DrawLine(pen, rect.Left + rect.Width * 0.2f, rect.Top + rect.Height / 2f, rect.Right - rect.Width * 0.2f, rect.Top + rect.Height / 2);
                        break;
                    case 2:
                        g.DrawLine(pen, rect.Left + rect.Width * 0.2f, rect.Top + rect.Height / 2f, rect.Right - rect.Width * 0.2f, rect.Top + rect.Height / 2);
                        break;
                }
                
            }
        }
    }
}
