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
    public class EventCollection
    {
        public List<BPMChangeEvent> BPMChangeEvents { get; set; } = new List<BPMChangeEvent>() { new BPMChangeEvent() { Tick = 0, BPM = 120 } };
    }
}
