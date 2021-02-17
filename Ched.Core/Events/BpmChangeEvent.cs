using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ched.Core.Events
{
    /// <summary>
    /// BPMの変更イベントを表すクラスです。
    /// </summary>
    [Newtonsoft.Json.JsonObject(Newtonsoft.Json.MemberSerialization.OptIn)]
    [DebuggerDisplay("Tick = {Tick}, Value = {Bpm}")]
    public class BpmChangeEvent : EventBase
    {
        [Newtonsoft.Json.JsonProperty]
        private decimal bpm;

        public decimal Bpm
        {
            get { return bpm; }
            set { bpm = value; }
        }
    }
}
