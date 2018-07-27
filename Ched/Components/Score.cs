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
    [Newtonsoft.Json.JsonObject(Newtonsoft.Json.MemberSerialization.OptIn)]
    public class Score
    {
        [Newtonsoft.Json.JsonProperty]
        private int ticksPerBeat = 480;
        [Newtonsoft.Json.JsonProperty]
        private NoteCollection notes = new NoteCollection();
        [Newtonsoft.Json.JsonProperty]
        private EventCollection events = new EventCollection();

        /// <summary>
        /// 1拍あたりの分解能を設定します。
        /// </summary>
        public int TicksPerBeat
        {
            get { return ticksPerBeat; }
            set { ticksPerBeat = value; }
        }

        /// <summary>
        /// ノーツを格納するコレクションです。
        /// </summary>
        public NoteCollection Notes
        {
            get { return notes; }
            set { notes = value; }
        }

        /// <summary>
        /// イベントを格納するコレクションです。
        /// </summary>
        public EventCollection Events
        {
            get { return events; }
            set { events = value; }
        }

        /// <summary>
        /// 指定の位置に時間を挿入します。
        /// </summary>
        /// <param name="position">時間を挿入する位置(Tick)</param>
        /// <param name="duration">挿入する時間(Tick)</param>
        public void InsertTicks(int position, int duration)
        {
            foreach (var note in Notes.GetShortNotes().Where(p => p.Tick >= position)) note.Tick += duration;
            foreach (var hold in Notes.Holds.Where(p => p.StartTick >= position)) hold.StartTick += duration;
            foreach (var slide in Notes.Slides.Where(p => p.StartTick >= position)) slide.StartTick += duration;
            foreach (var item in Events.GetAllEvents().Where(p => p.Tick >= position)) item.Tick += duration;
        }
    }
}
