using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Drawing;
using System.Drawing.Drawing2D;

namespace Ched.Components
{
    public class Air : ShortNoteBase
    {
        private static readonly Color ForegroundUpColor = Color.FromArgb(28, 206, 22);
        private static readonly Color ForegroundDownColor = Color.FromArgb(192, 21, 216);
        private static readonly Color BorderColor = Color.FromArgb(208, 208, 208);

        public VerticalAirDirection VerticalDirection { get; set; }
        public HorizontalAirDirection HorizontalDirection { get; set; }

        internal void Draw(Graphics g, RectangleF targetNoteRect) // 描画対象のノートのrect
        {
            var targetSize = new SizeF(targetNoteRect.Width * 0.9f, targetNoteRect.Height * 3);
            var targetLocation = new PointF(targetNoteRect.Left + targetNoteRect.Width * 0.05f, targetNoteRect.Top - targetSize.Height - targetNoteRect.Height);
            var targetRect = new RectangleF(targetLocation, targetSize);

            // ノートを内包するRect(ノートの下部中心が原点)
            var box = new RectangleF(-targetSize.Width / 2, -targetSize.Height, targetSize.Width, targetSize.Height);
            // ノート形状の構成点
            var points = new PointF[] {
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
                var prevMatrix = g.Transform;
                var matrix = new Matrix();

                // 描画先の下部中心を原点にもってくる
                matrix.Translate(targetRect.Left + targetRect.Width / 2, targetRect.Bottom);
                // 振り下げなら上下反転
                if (VerticalDirection == VerticalAirDirection.Down) matrix.Scale(1, -1);
                // 左右分で傾斜をかける
                if (HorizontalDirection != HorizontalAirDirection.Center) matrix.Shear(HorizontalDirection == HorizontalAirDirection.Left ? 0.5f : -0.5f, 0);
                // 振り下げでずれた分補正
                if (VerticalDirection == VerticalAirDirection.Down) matrix.Translate(0, box.Height);

                g.MultiplyTransform(matrix);

                using (var brush = new SolidBrush(VerticalDirection == VerticalAirDirection.Down ? ForegroundDownColor : ForegroundUpColor))
                {
                    g.FillPath(brush, path);
                }
                using (var pen = new Pen(BorderColor, targetRect.Height * 0.12f) { LineJoin = LineJoin.Bevel })
                {
                    g.DrawPath(pen, path);
                }
                g.Transform = prevMatrix;
            }
        }

    }

    public enum VerticalAirDirection
    {
        Up,
        Down
    }

    public enum HorizontalAirDirection
    {
        Center,
        Left,
        Right
    }
}
