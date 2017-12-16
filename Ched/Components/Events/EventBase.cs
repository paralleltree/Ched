using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ched.Components
{
    /// <summary>
    /// 譜面におけるイベントを表すクラスです。
    /// </summary>
    [Newtonsoft.Json.JsonObject(Newtonsoft.Json.MemberSerialization.OptIn)]
    public abstract class EventBase
    {
        [Newtonsoft.Json.JsonProperty]
        private int tick;

        public int Tick
        {
            get { return tick; }
            set
            {
                tick = value;
            }
        }
    }
}
