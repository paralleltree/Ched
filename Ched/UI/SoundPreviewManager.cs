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
    /// <remarks>
    /// 公開メソッドはUIスレッド経由での操作前提でスレッドセーフではない。
    /// </remarks>
    public class SoundPreviewManager : IDisposable
    {
        public event EventHandler<TickUpdatedEventArgs> TickUpdated;
        public event EventHandler Finished;
        public event EventHandler ExceptionThrown;

        private int CurrentTick { get; set; }
        private SoundSource ClapSource { get; set; }
        private SoundManager SoundManager { get; } = new SoundManager();
        private ISoundPreviewContext PreviewContext { get; set; }
        private LinkedListNode<int?> TickElement;
        private LinkedListNode<BpmChangeEvent> BpmElement;
        private int LastSystemTick { get; set; }
        private int InitialTick { get; set; }
        private int StartTick { get; set; }
        private int EndTick { get; set; }
        private double elapsedTick;
        private Control SyncControl { get; }
        private Timer Timer { get; } = new Timer() { Interval = 4 };

        public bool Playing { get; private set; }
        public bool IsStopAtLastNote { get; set; }
        public bool IsSupported { get { return SoundManager.IsSupported; } }

        public SoundPreviewManager(Control syncControl)
        {
            SyncControl = syncControl;
            ClapSource = new SoundSource("guide.mp3", 0.036);
            Timer.Tick += Tick;
            SoundManager.ExceptionThrown += (s, e) => SyncControl.InvokeIfRequired(() =>
            {
                Stop();
                ExceptionThrown?.Invoke(this, EventArgs.Empty);
            });
        }

        public bool Start(ISoundPreviewContext context, int startTick)
        {
            if (Playing) throw new InvalidOperationException();
            if (context == null) throw new ArgumentNullException("context");
            PreviewContext = context;
            SoundManager.Register(ClapSource.FilePath);
            SoundManager.Register(context.MusicSource.FilePath);

            var ticks = new SortedSet<int>(context.GetGuideTicks()).ToList();
            TickElement = new LinkedList<int?>(ticks.Where(p => p >= startTick).OrderBy(p => p).Select(p => new int?(p))).First;
            BpmElement = new LinkedList<BpmChangeEvent>(context.BpmDefinitions.OrderBy(p => p.Tick)).First;

            EndTick = IsStopAtLastNote ? ticks[ticks.Count - 1] : GetTickFromTime(SoundManager.GetDuration(context.MusicSource.FilePath), context.BpmDefinitions);
            if (EndTick < startTick) return false;

            // スタート時まで進める
            while (TickElement != null && TickElement.Value < startTick) TickElement = TickElement.Next;
            while (BpmElement.Next != null && BpmElement.Next.Value.Tick <= startTick) BpmElement = BpmElement.Next;

            int clapLatencyTick = GetLatencyTick(ClapSource.Latency, BpmElement.Value.Bpm);
            InitialTick = startTick - clapLatencyTick;
            CurrentTick = InitialTick;
            StartTick = startTick;

            TimeSpan startTime = GetTimeFromTick(startTick, context.BpmDefinitions);
            TimeSpan headGap = TimeSpan.FromSeconds(-context.MusicSource.Latency) - startTime;
            elapsedTick = 0;
            Task.Run(() =>
            {
                LastSystemTick = Environment.TickCount;
                SyncControl.Invoke((MethodInvoker)(() => Timer.Start()));

                System.Threading.Thread.Sleep(TimeSpan.FromSeconds(Math.Max(ClapSource.Latency, 0)));
                if (headGap.TotalSeconds > 0)
                {
                    System.Threading.Thread.Sleep(headGap);
                }
                if (!Playing) return;
                SoundManager.Play(context.MusicSource.FilePath, startTime + TimeSpan.FromSeconds(context.MusicSource.Latency));
            })
            .ContinueWith(p =>
            {
                if (p.Exception != null)
                {
                    Program.DumpExceptionTo(p.Exception, "sound_exception.json");
                    ExceptionThrown?.Invoke(this, EventArgs.Empty);
                }
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

            elapsedTick += PreviewContext.TicksPerBeat * BpmElement.Value.Bpm * elapsed / 60 / 1000;
            CurrentTick = (int)(InitialTick + elapsedTick);
            if (CurrentTick >= StartTick)
                TickUpdated?.Invoke(this, new TickUpdatedEventArgs(Math.Max(CurrentTick, 0)));

            while (BpmElement.Next != null && BpmElement.Next.Value.Tick <= CurrentTick) BpmElement = BpmElement.Next;

            if (CurrentTick >= EndTick + PreviewContext.TicksPerBeat)
            {
                Stop();
            }

            int latencyTick = GetLatencyTick(ClapSource.Latency, BpmElement.Value.Bpm);
            if (TickElement == null || TickElement.Value - latencyTick > CurrentTick) return;
            while (TickElement != null && TickElement.Value - latencyTick <= CurrentTick)
            {
                TickElement = TickElement.Next;
            }

            SoundManager.Play(ClapSource.FilePath);
        }

        private int GetLatencyTick(double latency, double bpm)
        {
            return (int)(PreviewContext.TicksPerBeat * latency * bpm / 60);
        }

        private TimeSpan GetLatencyTime(int tick, double bpm)
        {
            return TimeSpan.FromSeconds((double)tick * 60 / PreviewContext.TicksPerBeat / bpm);
        }

        private TimeSpan GetTimeFromTick(int tick, IEnumerable<BpmChangeEvent> bpmEvents)
        {
            var bpm = new LinkedList<BpmChangeEvent>(bpmEvents.OrderBy(p => p.Tick)).First;
            if (bpm.Value.Tick != 0) throw new ArgumentException("Initial BPM change event not found");

            var time = new TimeSpan();
            while (bpm.Next != null)
            {
                if (tick < bpm.Next.Value.Tick) break; // 現在のBPMで到達
                time += GetLatencyTime(bpm.Next.Value.Tick - bpm.Value.Tick, bpm.Value.Bpm);
                bpm = bpm.Next;
            }
            return time + GetLatencyTime(tick - bpm.Value.Tick, bpm.Value.Bpm);
        }

        private int GetTickFromTime(TimeSpan time, IEnumerable<BpmChangeEvent> bpmEvents)
        {
            var bpm = new LinkedList<BpmChangeEvent>(bpmEvents.OrderBy(p => p.Tick)).First;
            if (bpm.Value.Tick != 0) throw new ArgumentException("Initial BPM change event not found");

            TimeSpan sum = new TimeSpan();
            while (bpm.Next != null)
            {
                TimeSpan section = GetLatencyTime(bpm.Next.Value.Tick - bpm.Value.Tick, bpm.Value.Bpm);
                if (time < sum + section) break;
                sum += section;
                bpm = bpm.Next;
            }
            return bpm.Value.Tick + GetLatencyTick((time - sum).TotalSeconds, bpm.Value.Bpm);
        }


        public void Dispose()
        {
            SoundManager.Dispose();
        }
    }

    public interface ISoundPreviewContext
    {
        int TicksPerBeat { get; }
        IEnumerable<int> GetGuideTicks();
        IEnumerable<BpmChangeEvent> BpmDefinitions { get; }
        SoundSource MusicSource { get; }
    }

    public class SoundPreviewContext : ISoundPreviewContext
    {
        private Core.Score score;

        public int TicksPerBeat => score.TicksPerBeat;
        public IEnumerable<BpmChangeEvent> BpmDefinitions => score.Events.BpmChangeEvents;
        public SoundSource MusicSource { get; }

        public SoundPreviewContext(Core.Score score, SoundSource musicSource)
        {
            this.score = score;
            MusicSource = musicSource;
        }

        public IEnumerable<int> GetGuideTicks() => GetGuideTicks(score.Notes);

        private IEnumerable<int> GetGuideTicks(Core.NoteCollection notes)
        {
            var shortNotesTick = notes.Taps.Cast<TappableBase>().Concat(notes.ExTaps).Concat(notes.Flicks).Concat(notes.Damages).Select(p => p.Tick);
            var holdsTick = notes.Holds.SelectMany(p => new int[] { p.StartTick, p.StartTick + p.Duration });
            var slidesTick = notes.Slides.SelectMany(p => new int[] { p.StartTick }.Concat(p.StepNotes.Where(q => q.IsVisible).Select(q => q.Tick)));
            var airActionsTick = notes.AirActions.SelectMany(p => p.ActionNotes.Select(q => p.StartTick + q.Offset));

            return shortNotesTick.Concat(holdsTick).Concat(slidesTick).Concat(airActionsTick);
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
