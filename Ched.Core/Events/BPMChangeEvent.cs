﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ched.Core.Events
{
    /// <summary>
    /// BPMの変更イベントを表すクラスです。
    /// </summary>
    [Newtonsoft.Json.JsonObject(Newtonsoft.Json.MemberSerialization.OptIn)]
    public class BPMChangeEvent : EventBase
    {
        [Newtonsoft.Json.JsonProperty]
        private decimal bpm;

        public decimal BPM
        {
            get { return bpm; }
            set { bpm = value; }
        }
    }
}
