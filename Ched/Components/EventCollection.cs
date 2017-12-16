using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Ched.Components.Events;

namespace Ched.Components
{
    /// <summary>
    /// イベントを格納するコレクションを表すクラスです。
    /// </summary>
    [Newtonsoft.Json.JsonObject(Newtonsoft.Json.MemberSerialization.OptIn)]
    public class EventCollection
    {
        [Newtonsoft.Json.JsonProperty]
        private List<BPMChangeEvent> bpmChangeEvents = new List<BPMChangeEvent>() { new BPMChangeEvent() { Tick = 0, BPM = 120 } };

        public List<BPMChangeEvent> BPMChangeEvents
        {
            get { return bpmChangeEvents; }
            set { bpmChangeEvents = value; }
        }
    }
}
