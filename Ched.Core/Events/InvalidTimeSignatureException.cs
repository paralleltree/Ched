using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace Ched.Core.Events
{
    /// <summary>
    /// 拍子定義が無効な場合にスローされる例外です。
    /// </summary>
    [Serializable]
    public class InvalidTimeSignatureException : Exception
    {
        private static readonly string TickPropertyValue = "tick";

        /// <summary>
        /// 無効な拍子定義の位置を表すTick値を取得します。
        /// </summary>
        public int Tick { get; }

        public InvalidTimeSignatureException() : base()
        {
        }

        public InvalidTimeSignatureException(string message) : base(message)
        {
        }

        public InvalidTimeSignatureException(string message, Exception inner) : base(message, inner)
        {
        }

        public InvalidTimeSignatureException(string message, int tick) : this(message, tick, null)
        {
        }

        public InvalidTimeSignatureException(string message, int tick, Exception innerException) : base(message, innerException)
        {
            Tick = tick;
        }

        protected InvalidTimeSignatureException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
            if (info == null) return;
            Tick = info.GetInt32(TickPropertyValue);
        }

        public override void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            base.GetObjectData(info, context);
            if (info == null) return;

            info.AddValue(TickPropertyValue, Tick);
        }
    }
}
