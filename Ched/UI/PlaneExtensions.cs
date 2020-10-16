using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ched.UI
{
    public static class PlaneExtensions
    {
        public static RectangleF GetLeftThumb(this RectangleF rect, float widthRate, float minimumWidth)
        {
            return new RectangleF(rect.X, rect.Y, Math.Max(rect.Width * widthRate, minimumWidth), rect.Height);
        }

        public static RectangleF GetRightThumb(this RectangleF rect, float widthRate, float minimumWidth)
        {
            float width = Math.Max(rect.Width * widthRate, minimumWidth);
            return new RectangleF(rect.Right - width, rect.Y, width, rect.Height);
        }

        public static Matrix GetInvertedMatrix(this Matrix src)
        {
            var dest = src.Clone();
            dest.Invert();
            return dest;
        }

        public static Point TransformPoint(this Matrix matrix, Point point)
        {
            var arr = new Point[] { point };
            matrix.TransformPoints(arr);
            return arr.Single();
        }

        public static PointF TransformPoint(this Matrix matrix, PointF point)
        {
            var arr = new PointF[] { point };
            matrix.TransformPoints(arr);
            return arr.Single();
        }

        /// <summary>
        /// 凸状のポリゴン内に点が含まれているかどうか判定します。
        /// </summary>
        /// <param name="vertexes">頂点を格納した配列</param>
        /// <param name="point">判定する座標</param>
        /// <returns>座標がポリゴン内に含まれていればtrue</returns>
        /// <remarks>ref: http://blackpawn.com/texts/pointinpoly/default.html</remarks>
        public static bool ContainsPoint(this PointF[] vertexes, PointF point)
        {
            bool hitTriangle(PointF a, PointF b, PointF c, PointF p)
            {
                var ab = b.Subtract(a);
                var ac = c.Subtract(a);
                var ap = p.Subtract(a);

                float abab = ab.Dot(ab);
                float acac = ac.Dot(ac);
                float acap = ac.Dot(ap);
                float abac = ab.Dot(ac);
                float abap = ab.Dot(ap);

                float denom = acac * abab - abac * abac;

                float u = (acac * abap - abac * acap) / denom;
                float v = (abab * acap - abac * abap) / denom;

                return u >= 0 && v >= 0 && u + v < 1;
            }

            for (int i = 1; i <= vertexes.Length - 2; i++)
            {
                if (hitTriangle(vertexes[0], vertexes[i], vertexes[i + 1], point)) return true;
            }

            return false;
        }

        public static PointF Subtract(this PointF point, PointF offset)
        {
            return new PointF(point.X - offset.X, point.Y - offset.Y);
        }

        public static float Dot(this PointF point, PointF other)
        {
            return point.X * other.X + point.Y * other.Y;
        }
    }
}
