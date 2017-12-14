using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ched.Components.Events
{
    /// <summary>
    /// BPMの変更イベントを表すクラスです。
    /// </summary>
    [Newtonsoft.Json.JsonObject(Newtonsoft.Json.MemberSerialization.OptIn)]
    public class BPMChangeEvent : EventBase
    {
        [Newtonsoft.Json.JsonProperty]
        private float bpm;

        public float BPM
        {
            get { return bpm; }
            set { bpm = value; }
        }
    }
}
