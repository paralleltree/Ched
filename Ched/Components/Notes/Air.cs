using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Drawing;
using System.Drawing.Drawing2D;

namespace Ched.Components.Notes
{
    [Newtonsoft.Json.JsonObject(Newtonsoft.Json.MemberSerialization.OptIn)]
    public class Air : ShortNoteBase, IAirable
    {
        private static readonly Color ForegroundUpColor = Color.FromArgb(28, 206, 22);
        private static readonly Color ForegroundDownColor = Color.FromArgb(192, 21, 216);
        private static readonly Color BorderColor = Color.FromArgb(208, 208, 208);

        [Newtonsoft.Json.JsonProperty]
        private IAirable parentNote;
        [Newtonsoft.Json.JsonProperty]
        private VerticalAirDirection verticalDirection;
        [Newtonsoft.Json.JsonProperty]
        private HorizontalAirDirection horizontalDirection;

        public IAirable ParentNote { get { return parentNote; } }

        public VerticalAirDirection VerticalDirection
        {
            get { return verticalDirection; }
            set { verticalDirection = value; }
        }

        public HorizontalAirDirection HorizontalDirection
        {
            get { return horizontalDirection; }
            set { horizontalDirection = value; }
        }

        public int Tick { get { return ParentNote.Tick; } }

        public int LaneIndex { get { return ParentNote.LaneIndex; } }

        public int Width { get { return ParentNote.Width; } }

        public Air(IAirable parent)
        {
            parentNote = parent;
        }

        public void Flip()
        {
            if (HorizontalDirection == HorizontalAirDirection.Center) return;
            HorizontalDirection = HorizontalDirection == HorizontalAirDirection.Left ? HorizontalAirDirection.Right : HorizontalAirDirection.Left;
        }

        internal void Draw(Graphics g, RectangleF targetNoteRect) // 描画対象のノートのrect
        {
            RectangleF targetRect = GetDestRectangle(targetNoteRect);
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
                var prevMatrix = g.Transform;
                var matrix = prevMatrix.Clone();

                // 描画先の下部中心を原点にもってくる
                matrix.Translate(targetRect.Left + targetRect.Width / 2, targetRect.Top);
                // 振り上げなら上下反転(描画座標が上下逆になってるので……)
                if (VerticalDirection == VerticalAirDirection.Up) matrix.Scale(1, -1);
                // 左右分で傾斜をかける
                if (HorizontalDirection != HorizontalAirDirection.Center) matrix.Shear(HorizontalDirection == HorizontalAirDirection.Left ? 0.5f : -0.5f, 0);
                // 振り下げでずれた高さを補正
                if (VerticalDirection == VerticalAirDirection.Down) matrix.Translate(0, box.Height);

                g.Transform = matrix;

                using (var brush = new SolidBrush(VerticalDirection == VerticalAirDirection.Down ? ForegroundDownColor : ForegroundUpColor))
                {
                    g.FillPath(brush, path);
                }
                // 斜めになると太さが大きく出てしまう
                using (var pen = new Pen(BorderColor, targetRect.Height * (HorizontalDirection == HorizontalAirDirection.Center ? 0.12f : 0.1f)) { LineJoin = LineJoin.Bevel })
                {
                    g.DrawPath(pen, path);
                }

                g.Transform = prevMatrix;
            }
        }

        internal RectangleF GetDestRectangle(RectangleF targetNoteRect)
        {
            var targetSize = new SizeF(targetNoteRect.Width * 0.9f, targetNoteRect.Height * 3);
            var targetLocation = new PointF(targetNoteRect.Left + targetNoteRect.Width * 0.05f, targetNoteRect.Bottom + targetNoteRect.Height);
            return new RectangleF(targetLocation, targetSize);
        }
    }

    public interface IAirable
    {
        /// <summary>
        /// ノートの位置を表すTickを設定します。
        /// </summary>
        int Tick { get; }

        /// <summary>
        /// ノートの配置されるレーン番号を取得します。。
        /// </summary>
        int LaneIndex { get; }

        /// <summary>
        /// ノートのレーン幅を取得します。
        /// </summary>
        int Width { get; }
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
