using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ched.Components.Events
{
    /// <summary>
    /// ハイスピードの変更を表すクラスです。
    /// </summary>
    [Newtonsoft.Json.JsonObject(Newtonsoft.Json.MemberSerialization.OptIn)]
    public class HighSpeedChangeEvent : EventBase
    {
        [Newtonsoft.Json.JsonProperty]
        private decimal speedRatio;

        /// <summary>
        /// 1を基準とする速度比を設定します。
        /// </summary>
        public decimal SpeedRatio
        {
            get { return speedRatio; }
            set { speedRatio = value; }
        }
    }
}
