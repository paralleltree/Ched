using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ched.Drawing
{
    public class DrawingContext
    {
        public Graphics Graphics { get; }
        public ColorProfile ColorProfile { get; }

        public DrawingContext(Graphics g, ColorProfile colorProfile)
        {
            Graphics = g;
            ColorProfile = colorProfile;
        }
    }
}
