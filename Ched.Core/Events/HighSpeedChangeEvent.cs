using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ched.Core.Events
{
    /// <summary>
    /// ハイスピードの変更を表すクラスです。
    /// </summary>
    [Newtonsoft.Json.JsonObject(Newtonsoft.Json.MemberSerialization.OptIn)]
    [DebuggerDisplay("Tick = {Tick}, Value = {SpeedRatio}, Ch = {Channel}")]
    public class HighSpeedChangeEvent : EventBase
    {
        [Newtonsoft.Json.JsonProperty]
        private decimal speedRatio;
        [Newtonsoft.Json.JsonProperty]
        private int speedCh;



        /// <summary>
        /// 1を基準とする速度比を設定します。
        /// </summary>
        public decimal SpeedRatio
        {
            get { return speedRatio; }
            set { speedRatio = value; }
        }
        /// <summary>
        /// ハイスピードのチャンネルを設定します。
        /// </summary>
        public int SpeedCh
        {
            get { return speedCh; }
            set { speedCh = value; }
        }

    }
}
