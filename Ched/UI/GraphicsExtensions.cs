using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Drawing;
using System.Runtime.InteropServices;

namespace Ched.UI
{
    internal static class GraphicsExtensions
    {
        [DllImport("gdi32.dll")]
        private static extern int SetROP2(IntPtr hdc, int enDrawMode);

        [DllImport("gdi32.dll")]
        private static extern IntPtr CreatePen(PenStyles enPenStyle, int nWidth, int crColor);

        [DllImport("gdi32.dll")]
        private static extern bool DeleteObject(IntPtr hObject);

        [DllImport("gdi32.dll")]
        private static extern IntPtr SelectObject(IntPtr hdc, IntPtr hObject);

        [DllImport("gdi32.dll")]
        private static extern void Rectangle(IntPtr hdc, int x1, int y1, int x2, int y2);

        [DllImport("gdi32.dll")]
        private static extern IntPtr GetStockObject(int brStyle);

        private static int NULL_BRUSH = 5;
        private static int BLACK_PEN = 0;
        private static int R2_XORPEN = 7;

        // ref: https://www.codeproject.com/Articles/4958/Combining-GDI-and-GDI-to-Draw-Rubber-Band-Rectangl
        public static void DrawXorRectangle(this Graphics g, PenStyles style, int x1, int y1, int x2, int y2)
        {
            IntPtr hdc = g.GetHdc();
            IntPtr pen = CreatePen(style, 1, BLACK_PEN);

            SetROP2(hdc, R2_XORPEN);

            IntPtr oldPen = SelectObject(hdc, pen);
            IntPtr oldBrush = SelectObject(hdc, GetStockObject(NULL_BRUSH));

            Rectangle(hdc, x1, y1, x2, y2);

            SelectObject(hdc, oldBrush);
            SelectObject(hdc, oldPen);
            DeleteObject(pen);

            g.ReleaseHdc(hdc);
        }

        public static void DrawXorRectangle(this Graphics g, PenStyles style, Point start, Point end)
        {
            g.DrawXorRectangle(style, start.X, start.Y, end.X, end.Y);
        }
    }

    internal enum PenStyles
    {
        Solid = 0,
        Dash = 1,
        Dot = 2,
        DashDot = 3,
        DashDotDot = 4
    }
}
