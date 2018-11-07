using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ched.Drawing
{
    internal static class GraphicsExtensions
    {
        // ref: http://csharphelper.com/blog/2016/01/draw-rounded-rectangles-in-c/
        public static GraphicsPath ToRoundedPath(this RectangleF rect, float radius)
        {
            if (radius * 2 > Math.Min(rect.Width, rect.Height))
            {
                throw new ArgumentException("radius must be less than short side.", "radius");
            }

            var path = new GraphicsPath();

            path.AddArc(rect.Left, rect.Top, radius * 2, radius * 2, 180, 90);
            path.AddArc(rect.Right - radius * 2, rect.Top, radius * 2, radius * 2, 270, 90);
            path.AddArc(rect.Right - radius * 2, rect.Bottom - radius * 2, radius * 2, radius * 2, 0, 90);
            path.AddArc(rect.Left, rect.Bottom - radius * 2, radius * 2, radius * 2, 90, 90);
            path.CloseFigure();
            return path;
        }

        public static RectangleF Expand(this RectangleF rect, float size)
        {
            return rect.Expand(size, size);
        }

        public static RectangleF Expand(this RectangleF rect, float dx, float dy)
        {
            return new RectangleF(rect.Left - dx, rect.Top - dy, rect.Width + dx * 2, rect.Height + dy * 2);
        }
    }
}
