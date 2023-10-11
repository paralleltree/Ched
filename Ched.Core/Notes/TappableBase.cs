using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ched.Core.Notes
{
    [Newtonsoft.Json.JsonObject(Newtonsoft.Json.MemberSerialization.OptIn)]
    public abstract class TappableBase : IAirable
    {
        [Newtonsoft.Json.JsonProperty]
        private int tick;
        [Newtonsoft.Json.JsonProperty]
        private float laneIndex;
        [Newtonsoft.Json.JsonProperty]
        private float width = 1;
        [Newtonsoft.Json.JsonProperty]
        private int channel;
        [Newtonsoft.Json.JsonProperty]
        private bool isStart = false;

        /// <summary>
        /// ノートの位置を表すTickを設定します。
        /// </summary>
        public int Tick
        {
            get { return tick; }
            set
            {
                if (tick == value) return;
                if (tick < 0) throw new ArgumentOutOfRangeException("value", "value must not be negative.");
                tick = value;
            }
        }

        /// <summary>
        /// ノートの配置されるレーン番号を設定します。。
        /// </summary>
        public float LaneIndex
        {
            get { return laneIndex; }
            set
            {
                CheckPosition(value, Width);
                laneIndex = value;
            }
        }

        /// <summary>
        /// ノートのレーン幅を設定します。
        /// </summary>
        public float Width
        {
            get { return width; }
            set
            {
                CheckPosition(LaneIndex, value);
                width = value;
            }
        }

        /// <summary>
        /// ノートのハイスピードチャンネルを設定します。
        /// </summary>
        public int Channel
        {
            get { return channel; }
            set
            {
                channel = value;
            }
        }

        /// <summary>
        /// ノートのハイスピードチャンネルを設定します。
        /// </summary>
        public bool IsStart
        {
            get { return isStart; }
            set
            {
                isStart = value;
            }
        }

        protected void CheckPosition(float laneIndex, float width)
        {
            if (width < 0.1 )
                throw new ArgumentOutOfRangeException("width", "Invalid width.");

        }

        public void SetPosition(float laneIndex, float width)
        {
            CheckPosition(laneIndex, width);
            this.laneIndex = laneIndex;
            this.width = width;
        }
        public void SetChannel(int channel)
        {

            this.channel = channel;

        }
    }
}
