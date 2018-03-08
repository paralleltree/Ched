using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

using Ched.Components.Events;
using Ched.Components.Notes;

namespace Ched.UI
{
    public class SoundPreviewManager : IDisposable
    {
        private SoundSource ClapSource { get; set; }
        private SoundManager SoundManager { get; } = new SoundManager();
        private NoteView NoteView { get; set; }
        private LinkedListNode<int?> TickElement;
        private LinkedListNode<BPMChangeEvent> BPMElement;
        private int LastSystemTick { get; set; }
        private int StartTick { get; set; }
        private int EndTick { get; set; }
        private double elapsedTick;
        private Timer Timer { get; } = new Timer() { Interval = 4 };
        public bool Playing { get { return Timer.Enabled; } }

        public SoundPreviewManager(NoteView noteView)
        {
            ClapSource = new SoundSource("guide.mp3", 0.036);
            NoteView = noteView;
            Timer.Tick += Tick;
        }

        public bool Start(SoundSource music)
        {
            if (Playing) throw new InvalidOperationException();
            if (music == null) throw new ArgumentNullException("music");
            SoundManager.Register(ClapSource.FilePath);
            SoundManager.Register(music.FilePath);
            NoteView.CurrentTick = 0; // 再生位置計算めんどいので最初から
            StartTick = NoteView.CurrentTick;
            EndTick = NoteView.Notes.GetLastTick();
            if (EndTick < StartTick) return false;

            var tickSet = new HashSet<int>();
            var notes = NoteView.Notes;
            var shortNotesTick = notes.Taps.Cast<TappableBase>().Concat(notes.ExTaps).Concat(notes.Flicks).Concat(notes.Damages).Select(p => p.Tick);
            var holdsTick = notes.Holds.SelectMany(p => new int[] { p.StartTick, p.StartTick + p.Duration });
            var slidesTick = notes.Slides.SelectMany(p => new int[] { p.StartTick }.Concat(p.StepNotes.Where(q => q.IsVisible).Select(q => q.Tick)));
            var airActionsTick = notes.AirActions.SelectMany(p => p.ActionNotes.Select(q => p.StartTick + q.Offset));

            foreach (int tick in shortNotesTick.Concat(holdsTick).Concat(slidesTick).Concat(airActionsTick))
            {
                tickSet.Add(tick);
            }
            TickElement = new LinkedList<int?>(tickSet.Where(p => p >= StartTick).OrderBy(p => p).Select(p => new int?(p))).First;
            if (TickElement == null) return false; // 鳴らす対象ノーツがない

            BPMElement = new LinkedList<BPMChangeEvent>(NoteView.ScoreEvents.BPMChangeEvents.OrderBy(p => p.Tick)).First;

            // スタート時まで進める
            while (TickElement.Value < StartTick && TickElement.Next != null) TickElement = TickElement.Next;
            while (BPMElement.Value.Tick < StartTick && BPMElement.Next != null) BPMElement = BPMElement.Next;

            // BGM再生開始
            SoundManager.Play(music.FilePath);

            // BGMの遅延時間だけ待機してから時間を進めていく
            Task.Delay(TimeSpan.FromSeconds(Math.Max(music.Latency, ClapSource.Latency))).ContinueWith(p =>
            {
                NoteView.Invoke((MethodInvoker)(() =>
                {
                    LastSystemTick = Environment.TickCount;
                    elapsedTick = 0;
                    Timer.Start();
                }));
            });
            NoteView.Editable = false;
            return true;
        }

        public void Stop()
        {
            Timer.Stop();
            NoteView.Editable = true;
            SoundManager.StopAll();
        }

        private void Tick(object sender, EventArgs e)
        {
            int now = Environment.TickCount;
            int elapsed = now - LastSystemTick;
            LastSystemTick = now;

            elapsedTick += NoteView.UnitBeatTick * (double)BPMElement.Value.BPM * elapsed / 60 / 1000;
            NoteView.CurrentTick = (int)(StartTick + elapsedTick);

            while (BPMElement.Next != null && BPMElement.Value.Tick <= NoteView.CurrentTick) BPMElement = BPMElement.Next;

            if (NoteView.CurrentTick >= EndTick + NoteView.UnitBeatTick)
            {
                NoteView.Invoke((MethodInvoker)(() => Stop()));
            }

            int latencyTick = GetLatencyTick(ClapSource.Latency, (double)BPMElement.Value.BPM);
            if (TickElement == null || TickElement.Value - latencyTick > NoteView.CurrentTick) return;
            while (TickElement != null && TickElement.Value - latencyTick <= NoteView.CurrentTick)
            {
                TickElement = TickElement.Next;
            }

            SoundManager.Play(ClapSource.FilePath);
        }

        private int GetLatencyTick(double latency, double bpm)
        {
            return (int)(NoteView.UnitBeatTick * latency * bpm / 60);
        }


        public void Dispose()
        {
            SoundManager.Dispose();
        }
    }
}
