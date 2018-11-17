using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

using Ched.Core.Events;
using Ched.Core.Notes;

namespace Ched.UI
{
    public class SoundPreviewManager : IDisposable
    {
        public event EventHandler<TickUpdatedEventArgs> TickUpdated;
        public event EventHandler Finished;

        private int CurrentTick { get; set; }
        private SoundSource ClapSource { get; set; }
        private SoundManager SoundManager { get; } = new SoundManager();
        private NoteView NoteView { get; set; }
        private LinkedListNode<int?> TickElement;
        private LinkedListNode<BPMChangeEvent> BPMElement;
        private int LastSystemTick { get; set; }
        private int InitialTick { get; set; }
        private int StartTick { get; set; }
        private int EndTick { get; set; }
        private double elapsedTick;
        private Timer Timer { get; } = new Timer() { Interval = 4 };

        public bool Playing { get; private set; }
        public bool IsStopAtLastNote { get; set; }
        public bool IsSupported { get { return SoundManager.IsSupported; } }

        public SoundPreviewManager(NoteView noteView)
        {
            ClapSource = new SoundSource("guide.mp3", 0.036);
            NoteView = noteView;
            Timer.Tick += Tick;
        }

        public bool Start(SoundSource music)
        {
            return Start(music, 0); // 再生位置計算めんどいので最初から
        }

        public bool Start(SoundSource music, int startTick)
        {
            if (Playing) throw new InvalidOperationException();
            if (music == null) throw new ArgumentNullException("music");
            SoundManager.Register(ClapSource.FilePath);
            SoundManager.Register(music.FilePath);
            EndTick = IsStopAtLastNote ? NoteView.Notes.GetLastTick() : GetTickFromTime(SoundManager.GetDuration(music.FilePath), NoteView.ScoreEvents.BPMChangeEvents);
            if (EndTick < startTick) return false;

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
            TickElement = new LinkedList<int?>(tickSet.Where(p => p >= startTick).OrderBy(p => p).Select(p => new int?(p))).First;

            BPMElement = new LinkedList<BPMChangeEvent>(NoteView.ScoreEvents.BPMChangeEvents.OrderBy(p => p.Tick)).First;

            // スタート時まで進める
            while (TickElement != null && TickElement.Value < startTick) TickElement = TickElement.Next;
            while (BPMElement.Next != null && BPMElement.Next.Value.Tick <= startTick) BPMElement = BPMElement.Next;

            int clapLatencyTick = GetLatencyTick(ClapSource.Latency, (double)BPMElement.Value.BPM);
            InitialTick = startTick - clapLatencyTick;
            CurrentTick = InitialTick;
            StartTick = startTick;

            TimeSpan startTime = GetTimeFromTick(startTick, NoteView.ScoreEvents.BPMChangeEvents);
            Task.Run(() =>
            {
                LastSystemTick = Environment.TickCount;
                elapsedTick = 0;
                NoteView.Invoke((MethodInvoker)(() => Timer.Start()));

                System.Threading.Thread.Sleep(TimeSpan.FromSeconds(Math.Max(ClapSource.Latency, 0)));
                SoundManager.Play(music.FilePath, startTime + TimeSpan.FromSeconds(music.Latency));
            });

            Playing = true;
            return true;
        }

        public void Stop()
        {
            Timer.Stop();
            Playing = false;
            SoundManager.StopAll();
            Finished?.Invoke(this, EventArgs.Empty);
        }

        private void Tick(object sender, EventArgs e)
        {
            int now = Environment.TickCount;
            int elapsed = now - LastSystemTick;
            LastSystemTick = now;

            elapsedTick += NoteView.UnitBeatTick * (double)BPMElement.Value.BPM * elapsed / 60 / 1000;
            CurrentTick = (int)(InitialTick + elapsedTick);
            if (CurrentTick >= StartTick)
                TickUpdated?.Invoke(this, new TickUpdatedEventArgs(Math.Max(CurrentTick, 0)));

            while (BPMElement.Next != null && BPMElement.Next.Value.Tick <= CurrentTick) BPMElement = BPMElement.Next;

            if (NoteView.CurrentTick >= EndTick + NoteView.UnitBeatTick)
            {
                Stop();
            }

            int latencyTick = GetLatencyTick(ClapSource.Latency, (double)BPMElement.Value.BPM);
            if (TickElement == null || TickElement.Value - latencyTick > CurrentTick) return;
            while (TickElement != null && TickElement.Value - latencyTick <= CurrentTick)
            {
                TickElement = TickElement.Next;
            }

            SoundManager.Play(ClapSource.FilePath);
        }

        private int GetLatencyTick(double latency, double bpm)
        {
            return (int)(NoteView.UnitBeatTick * latency * bpm / 60);
        }

        private TimeSpan GetLatencyTime(int tick, double bpm)
        {
            return TimeSpan.FromSeconds((double)tick * 60 / NoteView.UnitBeatTick / bpm);
        }

        private TimeSpan GetTimeFromTick(int tick, IEnumerable<BPMChangeEvent> bpmEvents)
        {
            var bpm = new LinkedList<BPMChangeEvent>(bpmEvents.OrderBy(p => p.Tick)).First;
            if (bpm.Value.Tick != 0) throw new ArgumentException("Initial BPM change event not found");

            var time = new TimeSpan();
            while (bpm.Next != null)
            {
                if (tick < bpm.Next.Value.Tick) break; // 現在のBPMで到達
                time += GetLatencyTime(bpm.Next.Value.Tick - bpm.Value.Tick, (double)bpm.Value.BPM);
                bpm = bpm.Next;
            }
            return time + GetLatencyTime(tick - bpm.Value.Tick, (double)bpm.Value.BPM);
        }

        private int GetTickFromTime(TimeSpan time, IEnumerable<BPMChangeEvent> bpmEvents)
        {
            var bpm = new LinkedList<BPMChangeEvent>(bpmEvents.OrderBy(p => p.Tick)).First;
            if (bpm.Value.Tick != 0) throw new ArgumentException("Initial BPM change event not found");

            TimeSpan sum = new TimeSpan();
            while (bpm.Next != null)
            {
                TimeSpan section = GetLatencyTime(bpm.Next.Value.Tick - bpm.Value.Tick, (double)bpm.Value.BPM);
                if (time < sum + section) break;
                sum += section;
                bpm = bpm.Next;
            }
            return bpm.Value.Tick + GetLatencyTick((time - sum).TotalSeconds, (double)bpm.Value.BPM);
        }


        public void Dispose()
        {
            SoundManager.Dispose();
        }
    }

    public class TickUpdatedEventArgs : EventArgs
    {
        public int Tick { get; }

        public TickUpdatedEventArgs(int tick)
        {
            Tick = tick;
        }
    }
}
