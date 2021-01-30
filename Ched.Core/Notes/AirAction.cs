using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ched.Core.Notes
{
    [Newtonsoft.Json.JsonObject(Newtonsoft.Json.MemberSerialization.OptIn)]
    public class AirAction : ILongNote
    {
        [Newtonsoft.Json.JsonProperty]
        private IAirable parentNote;
        [Newtonsoft.Json.JsonProperty]
        private List<ActionNote> actionNotes = new List<ActionNote>();

        public List<ActionNote> ActionNotes { get { return actionNotes; } }
        public IAirable ParentNote { get { return parentNote; } }
        public int StartTick => ParentNote.Tick;

        /// <summary>
        /// 親<see cref="IAirable"/>オブジェクトを持たない<see cref="AirAction"/>の新しいインスタンスを初期化します。
        /// </summary>
        /// <remarks>このコンストラクタはシリアライザ用に存在します。</remarks>
        public AirAction()
        {
        }

        /// <summary>
        /// 指定の<see cref="IAirable"/>を親とする<see cref="AirAction"/>の新しいインスタンスを初期化します。
        /// </summary>
        /// <param name="parent">この<see cref="AirAction"/>の親となる<see cref="IAirable"/>オブジェクト</param>
        public AirAction(IAirable parent)
        {
            parentNote = parent;
        }

        public int GetDuration()
        {
            return ActionNotes.Max(p => p.Offset);
        }


        [Newtonsoft.Json.JsonObject(Newtonsoft.Json.MemberSerialization.OptIn)]
        public class ActionNote
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

            /// <summary>
            /// 指定の<see cref="AirAction"/>を親とする<see cref="ActionNote"/>の新しいインスタンスを初期化します。
            /// </summary>
            /// <param name="parent">この<see cref="ActionNote"/>の親となる<see cref="AirAction"/>オブジェクト</param>
            public ActionNote(AirAction parent)
            {
                parentNote = parent;
            }
        }
    }
}
