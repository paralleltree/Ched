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
    public class AirAction : LongNoteBase
    {
        private static readonly Color LineColor = Color.FromArgb(216, 0, 196, 0);

        [Newtonsoft.Json.JsonProperty]
        private IAirable parentNote;
        [Newtonsoft.Json.JsonProperty]
        private List<ActionNote> actionNotes = new List<ActionNote>();

        public List<ActionNote> ActionNotes { get { return actionNotes; } }
        public IAirable ParentNote { get { return parentNote; } }
        public override int StartTick { get { return ParentNote.Tick; } }

        public AirAction(IAirable parent)
        {
            parentNote = parent;
        }

        public override int GetDuration()
        {
            return ActionNotes.Max(p => p.Offset);
        }

        internal void DrawLine(Graphics g, float x, float y1, float y2, float noteHeight)
        {
            using (var pen = new Pen(LineColor, noteHeight / 2))
            {
                g.DrawLine(pen, x, y1, x, y2);
            }
        }

        [Newtonsoft.Json.JsonObject(Newtonsoft.Json.MemberSerialization.OptIn)]
        public class ActionNote : ShortNoteBase
        {
            private readonly Color LightNoteColor = Color.FromArgb(212, 92, 255);
            private readonly Color DarkNoteColor = Color.FromArgb(146, 0, 192);

            [Newtonsoft.Json.JsonProperty]
            private int offset;
            [Newtonsoft.Json.JsonProperty]
            private AirAction parentNote;

            public AirAction ParentNote { get { return parentNote; } }

            public int Offset
            {
                get { return offset; }
                set
                {
                    if (value <= 0) throw new ArgumentOutOfRangeException("value", "value must be positive.");
                    offset = value;
                }
            }

            public ActionNote(AirAction parent)
            {
                parentNote = parent;
            }

            internal void Draw(Graphics g, RectangleF rect)
            {
                using (var brush = new LinearGradientBrush(rect, DarkNoteColor, LightNoteColor, LinearGradientMode.Vertical))
                {
                    g.FillRectangle(brush, rect);
                }
                using (var brush = new LinearGradientBrush(rect.Expand(rect.Height * 0.1f), DarkBorderColor, LightBorderColor, LinearGradientMode.Vertical))
                {
                    using (var pen = new Pen(brush, rect.Height * 0.1f))
                    {
                        g.DrawRectangle(pen, rect.X, rect.Y, rect.Width, rect.Height);
                    }
                }
            }
        }
    }
}
