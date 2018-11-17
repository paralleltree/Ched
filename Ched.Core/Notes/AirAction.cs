using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ched.Core.Notes
{
    [Newtonsoft.Json.JsonObject(Newtonsoft.Json.MemberSerialization.OptIn)]
    public class AirAction : LongNoteBase
    {
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


        [Newtonsoft.Json.JsonObject(Newtonsoft.Json.MemberSerialization.OptIn)]
        public class ActionNote : ShortNoteBase
        {
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
        }
    }
}
