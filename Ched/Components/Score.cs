using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ched.Components
{
    /// <summary>
    /// 譜面データを表すクラスです。
    /// </summary>
    public class Score
    {
        /// <summary>
        /// 1拍あたりの分解能を設定します。
        /// </summary>
        public int TicksPerBeat { get; set; } = 480;

        /// <summary>
        /// ノーツを格納するコレクションです。
        /// </summary>
        public NoteCollection Notes { get; set; } = new NoteCollection();

        /// <summary>
        /// イベントを格納するコレクションです。
        /// </summary>
        public EventCollection Events { get; set; } = new EventCollection();
    }
}
