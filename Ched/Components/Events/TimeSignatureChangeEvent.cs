using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ched.Components.Events
{
    /// <summary>
    /// 拍子の変更を表します。
    /// </summary>
    [Newtonsoft.Json.JsonObject(Newtonsoft.Json.MemberSerialization.OptIn)]
    public class TimeSignatureChangeEvent : EventBase
    {
        [Newtonsoft.Json.JsonProperty]
        private int numerator;
        [Newtonsoft.Json.JsonProperty]
        private int denominatorExponent;

        /// <summary>
        /// 拍子の分子を設定します。
        /// </summary>
        public int Numerator
        {
            get { return numerator; }
            set
            {
                if (value < 1) throw new ArgumentOutOfRangeException("value must be greater than 0.");
                numerator = value;
            }
        }

        /// <summary>
        /// 拍子の分母を取得します。
        /// </summary>
        public int Denominator
        {
            get
            {
                int p = 1;
                for (int i = 0; i < DenominatorExponent; i++) p *= 2;
                return p;
            }
        }

        /// <summary>
        /// 2を底とする拍子の分母の指数を設定します。
        /// </summary>
        public int DenominatorExponent
        {
            get { return denominatorExponent; }
            set
            {
                if (value < 0) throw new ArgumentOutOfRangeException("value must be positive.");
                denominatorExponent = value;
            }
        }
    }
}
