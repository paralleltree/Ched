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
    public class BPMChangeEvent : EventBase
    {
        public int BPM { get; set; }
    }
}
