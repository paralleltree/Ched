﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Ched.Core.Events;

namespace Ched.Core
{
    /// <summary>
    /// イベントを格納するコレクションを表すクラスです。
    /// </summary>
    [Newtonsoft.Json.JsonObject(Newtonsoft.Json.MemberSerialization.OptIn)]
    public class EventCollection
    {
        [Newtonsoft.Json.JsonProperty]
        private List<BPMChangeEvent> bpmChangeEvents = new List<BPMChangeEvent>() { new BPMChangeEvent() { Tick = 0, BPM = 120 } };
        [Newtonsoft.Json.JsonProperty]
        private List<TimeSignatureChangeEvent> timeSignatureChangeEvents = new List<TimeSignatureChangeEvent>() { new TimeSignatureChangeEvent() { Tick = 0, Numerator = 4, DenominatorExponent = 2 } };
        [Newtonsoft.Json.JsonProperty]
        private List<HighSpeedChangeEvent> highSpeedChangeEvents = new List<HighSpeedChangeEvent>();

        public List<BPMChangeEvent> BPMChangeEvents
        {
            get { return bpmChangeEvents; }
            set { bpmChangeEvents = value; }
        }

        public List<TimeSignatureChangeEvent> TimeSignatureChangeEvents
        {
            get { return timeSignatureChangeEvents; }
            set { timeSignatureChangeEvents = value; }
        }

        public List<HighSpeedChangeEvent> HighSpeedChangeEvents
        {
            get { return highSpeedChangeEvents; }
            set { highSpeedChangeEvents = value; }
        }
    }
}
