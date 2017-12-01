using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ched.Components
{
    public abstract class LongNoteBase : NoteBase
    {
    }

    public abstract class LongNoteTapBase : TappableBase
    {
        public bool IsTap { get; set; }

        internal override void Draw(Graphics g, RectangleF rect)
        {
            base.Draw(g, rect);
            if (IsTap) DrawTapSymbol(g, rect);
        }
    }
}
