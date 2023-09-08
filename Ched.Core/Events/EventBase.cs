using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ched.Core.Events
{
    /// <summary>
    /// 譜面におけるイベントを表すクラスです。
    /// </summary>
    [Newtonsoft.Json.JsonObject(Newtonsoft.Json.MemberSerialization.OptIn)]
    [DebuggerDisplay("Tick = {Tick}, Type = {Type}")]
    public abstract class EventBase
    {
        [Newtonsoft.Json.JsonProperty]
        private int tick;
        [Newtonsoft.Json.JsonProperty]
        private int type;

        /// <summary>
        /// このイベントの位置を表すTick値を取得、設定します。
        /// </summary>
        public int Tick
        {
            get { return tick; }
            set
            {
                if (value < 0) throw new ArgumentOutOfRangeException("value", "Tick must be greater than or equal to 0.");
                tick = value;
            }
        }

        public int Type
        {
            get { return type; }
            set
            {
                type = value;
            }
        }
    }
}
