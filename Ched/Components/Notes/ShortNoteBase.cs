using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Drawing;
using System.Drawing.Drawing2D;

namespace Ched.Components.Notes
{
    public abstract class ShortNoteBase : NoteBase
    {
        protected static readonly Color DarkBorderColor = Color.FromArgb(160, 160, 160);
        protected static readonly Color LightBorderColor = Color.FromArgb(208, 208, 208);
    }
}
