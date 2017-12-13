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
    public abstract class EventBase
    {
        public int Tick { get; set; }
    }
}
