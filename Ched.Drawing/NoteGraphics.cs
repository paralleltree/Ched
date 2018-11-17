using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Threading.Tasks;

using Ched.Core.Notes;

namespace Ched.Drawing
{
    public static class NoteGraphics
    {
        public static void DrawTap(this DrawingContext dc, RectangleF rect)
        {
            dc.Graphics.DrawTappableNote(rect, dc.ColorProfile.TapColor, dc.ColorProfile.BorderColor);
        }

        public static void DrawExTap(this DrawingContext dc, RectangleF rect)
        {
            dc.Graphics.DrawTappableNote(rect, dc.ColorProfile.ExTapColor, dc.ColorProfile.BorderColor);
        }

        public static void DrawFlick(this DrawingContext dc, RectangleF rect)
        {
            var foregroundRect = new RectangleF(rect.Left + rect.Width / 4, rect.Top, rect.Width / 2, rect.Height);
            dc.Graphics.DrawNoteBase(rect, dc.ColorProfile.FlickColor.Item1);
            dc.Graphics.DrawNoteBase(foregroundRect, dc.ColorProfile.FlickColor.Item2);
            dc.Graphics.DrawBorder(rect, dc.ColorProfile.BorderColor);
            dc.Graphics.DrawTapSymbol(foregroundRect);
        }

        public static void DrawDamage(this DrawingContext dc, RectangleF rect)
        {
            dc.Graphics.DrawSquarishNote(rect, dc.ColorProfile.DamageColor, dc.ColorProfile.BorderColor);
        }

        public static void DrawHoldBegin(this DrawingContext dc, RectangleF rect)
        {
            dc.DrawHoldEnd(rect);
            dc.Graphics.DrawTapSymbol(rect);
        }

        public static void DrawHoldEnd(this DrawingContext dc, RectangleF rect)
        {
            dc.Graphics.DrawNote(rect, dc.ColorProfile.HoldColor, dc.ColorProfile.BorderColor);
        }

        public static void DrawHoldBackground(this DrawingContext dc, RectangleF rect)
        {
            Color BackgroundEdgeColor = dc.ColorProfile.HoldBackgroundColor.DarkColor;
            Color BackgroundMiddleColor = dc.ColorProfile.HoldBackgroundColor.LightColor;

            var prevMode = dc.Graphics.SmoothingMode;
            dc.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            using (var brush = new LinearGradientBrush(rect, BackgroundEdgeColor, BackgroundMiddleColor, LinearGradientMode.Vertical))
            {
                var blend = new ColorBlend(4)
                {
                    Colors = new Color[] { BackgroundEdgeColor, BackgroundMiddleColor, BackgroundMiddleColor, BackgroundEdgeColor },
                    Positions = new float[] { 0.0f, 0.3f, 0.7f, 1.0f }
                };
                brush.InterpolationColors = blend;
                dc.Graphics.FillRectangle(brush, rect);
            }
            dc.Graphics.SmoothingMode = prevMode;
        }

        public static void DrawSlideBegin(this DrawingContext dc, RectangleF rect)
        {
            dc.DrawSlideStep(rect);
            dc.Graphics.DrawTapSymbol(rect);
        }

        public static void DrawSlideStep(this DrawingContext dc, RectangleF rect)
        {
            dc.Graphics.DrawNote(rect, dc.ColorProfile.SlideColor, dc.ColorProfile.BorderColor);
        }

        /// <summary>
        /// SLIDEの背景を描画します。
        /// </summary>
        /// <param name="g">描画先Graphics</param>
        /// <param name="width1">ノートの描画幅</param>
        /// <param name="x1">開始ノートの左端位置</param>
        /// <param name="y1">開始ノートのY座標</param>
        /// <param name="x2">終了ノートの左端位置</param>
        /// <param name="y2">終了ノートのY座標</param>
        /// <param name="gradStartY">始点Step以前の中継点のY座標(グラデーション描画用)</param>
        /// <param name="gradEndY">終点Step以後の中継点のY座標(グラデーション描画用)</param>
        /// <param name="noteHeight">ノートの描画高さ</param>
        public static void DrawSlideBackground(this DrawingContext dc, float width1, float width2, float x1, float y1, float x2, float y2, float gradStartY, float gradEndY, float noteHeight)
        {
            Color BackgroundEdgeColor = dc.ColorProfile.SlideBackgroundColor.DarkColor;
            Color BackgroundMiddleColor = dc.ColorProfile.SlideBackgroundColor.LightColor;

            var prevMode = dc.Graphics.SmoothingMode;
            dc.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            var rect = new RectangleF(Math.Min(x1, x2), Math.Min(y1, y2), Math.Abs(x1 - x2) + width1, Math.Abs(y1 - y2));
            var gradientRect = new RectangleF(rect.Left, gradStartY, rect.Width, gradEndY - gradStartY);
            using (var brush = new LinearGradientBrush(gradientRect, BackgroundEdgeColor, BackgroundMiddleColor, LinearGradientMode.Vertical))
            {
                var blend = new ColorBlend(4)
                {
                    Colors = new Color[] { BackgroundEdgeColor, BackgroundMiddleColor, BackgroundMiddleColor, BackgroundEdgeColor },
                    Positions = new float[] { 0.0f, 0.3f, 0.7f, 1.0f }
                };
                brush.InterpolationColors = blend;
                using (var path = GetSlideBackgroundPath(width1, width2, x1, y1, x2, y2))
                {
                    dc.Graphics.FillPath(brush, path);
                }
            }
            using (var pen = new Pen(dc.ColorProfile.SlideLineColor, noteHeight * 0.4f))
            {
                dc.Graphics.DrawLine(pen, x1 + width1 / 2, y1, x2 + width2 / 2, y2);
            }
            dc.Graphics.SmoothingMode = prevMode;
        }

        public static GraphicsPath GetSlideBackgroundPath(float width1, float width2, float x1, float y1, float x2, float y2)
        {
            var path = new GraphicsPath();
            path.AddPolygon(new PointF[]
            {
                new PointF(x1, y1),
                new PointF(x1 + width1, y1),
                new PointF(x2 + width2, y2),
                new PointF(x2, y2)
            });
            return path;
        }

        public static void DrawAir(this DrawingContext dc, RectangleF targetNoteRect, VerticalAirDirection verticalDirection, HorizontalAirDirection horizontalDirection)
        {
            var targetRect = GetAirRect(targetNoteRect);
            // ノートを内包するRect(ノートの下部中心が原点)
            var box = new RectangleF(-targetRect.Width / 2, -targetRect.Height, targetRect.Width, targetRect.Height);
            // ノート形状の構成点(上向き)
            var points = new PointF[]
            {
                new PointF(box.Left, box.Bottom),
                new PointF(box.Left, box.Top + box.Height / 3),
                new PointF(box.Left + box.Width / 2 , box.Top),
                new PointF(box.Right, box.Top + box.Height / 3),
                new PointF(box.Right, box.Bottom),
                new PointF(box.Left + box.Width / 2, box.Bottom - box.Height / 3)
            };

            using (var path = new GraphicsPath())
            {
                path.AddPolygon(points);
                var prevMatrix = dc.Graphics.Transform;
                var matrix = prevMatrix.Clone();

                // 描画先の下部中心を原点にもってくる
                matrix.Translate(targetRect.Left + targetRect.Width / 2, targetRect.Top);
                // 振り上げなら上下反転(描画座標が上下逆になってるので……)
                if (verticalDirection == VerticalAirDirection.Up) matrix.Scale(1, -1);
                // 左右分で傾斜をかける
                if (horizontalDirection != HorizontalAirDirection.Center) matrix.Shear(horizontalDirection == HorizontalAirDirection.Left ? 0.5f : -0.5f, 0);
                // 振り下げでずれた高さを補正
                if (verticalDirection == VerticalAirDirection.Down) matrix.Translate(0, box.Height);

                dc.Graphics.Transform = matrix;

                using (var brush = new SolidBrush(verticalDirection == VerticalAirDirection.Down ? dc.ColorProfile.AirDownColor : dc.ColorProfile.AirUpColor))
                {
                    dc.Graphics.FillPath(brush, path);
                }
                // 斜めになると太さが大きく出てしまう
                using (var pen = new Pen(dc.ColorProfile.BorderColor.LightColor, targetRect.Height * (horizontalDirection == HorizontalAirDirection.Center ? 0.12f : 0.1f)) { LineJoin = LineJoin.Bevel })
                {
                    dc.Graphics.DrawPath(pen, path);
                }

                dc.Graphics.Transform = prevMatrix;
            }
        }

        public static RectangleF GetAirRect(RectangleF targetNoteRect)
        {
            var targetSize = new SizeF(targetNoteRect.Width * 0.9f, targetNoteRect.Height * 3);
            var targetLocation = new PointF(targetNoteRect.Left + targetNoteRect.Width * 0.05f, targetNoteRect.Bottom + targetNoteRect.Height);
            return new RectangleF(targetLocation, targetSize);
        }

        public static void DrawAirAction(this DrawingContext dc, RectangleF rect)
        {
            using (var brush = new LinearGradientBrush(rect, dc.ColorProfile.AirActionColor.DarkColor, dc.ColorProfile.AirActionColor.LightColor, LinearGradientMode.Vertical))
            {
                dc.Graphics.FillRectangle(brush, rect);
            }
            using (var brush = new LinearGradientBrush(rect.Expand(rect.Height * 0.1f), dc.ColorProfile.BorderColor.DarkColor, dc.ColorProfile.BorderColor.LightColor, LinearGradientMode.Vertical))
            {
                using (var pen = new Pen(brush, rect.Height * 0.1f))
                {
                    dc.Graphics.DrawRectangle(pen, rect.X, rect.Y, rect.Width, rect.Height);
                }
            }
        }

        public static void DrawAirStep(this DrawingContext dc, RectangleF rect)
        {
            dc.Graphics.DrawNote(rect, dc.ColorProfile.AirStepColor, dc.ColorProfile.BorderColor);
        }

        public static void DrawAirHoldLine(this DrawingContext dc, float x, float y1, float y2, float noteHeight)
        {
            using (var pen = new Pen(dc.ColorProfile.AirHoldLineColor, noteHeight / 2))
            {
                dc.Graphics.DrawLine(pen, x, y1, x, y2);
            }
        }

        public static void DrawBorder(this DrawingContext dc, RectangleF rect)
        {
            dc.Graphics.DrawBorder(rect, dc.ColorProfile.BorderColor);
        }
    }
}
