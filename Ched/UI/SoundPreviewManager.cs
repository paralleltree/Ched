using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

using Ched.Core;
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
        public event EventHandler Started;
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

            var timeCalculator = new TimeCalculator(context.TicksPerBeat, context.BpmDefinitions);
            var ticks = new SortedSet<int>(context.GetGuideTicks()).ToList();
            TickElement = new LinkedList<int?>(ticks.Where(p => p >= startTick).OrderBy(p => p).Select(p => new int?(p))).First;
            BpmElement = new LinkedList<BpmChangeEvent>(context.BpmDefinitions.OrderBy(p => p.Tick)).First;

            EndTick = IsStopAtLastNote ? ticks[ticks.Count - 1] : timeCalculator.GetTickFromTime(SoundManager.GetDuration(context.MusicSource.FilePath));
            if (EndTick < startTick) return false;

            // スタート時まで進める
            while (TickElement != null && TickElement.Value < startTick) TickElement = TickElement.Next;
            while (BpmElement.Next != null && BpmElement.Next.Value.Tick <= startTick) BpmElement = BpmElement.Next;

            int clapLatencyTick = GetLatencyTick(ClapSource.Latency, BpmElement.Value.Bpm);
            InitialTick = startTick - clapLatencyTick;
            CurrentTick = InitialTick;
            StartTick = startTick;

            double startTime = timeCalculator.GetTimeFromTick(startTick);
            double headGap = -context.MusicSource.Latency - startTime;
            elapsedTick = 0;
            Task.Run(() =>
            {
                LastSystemTick = Environment.TickCount;
                SyncControl.Invoke((MethodInvoker)(() => Timer.Start()));

                System.Threading.Thread.Sleep(TimeSpan.FromSeconds(Math.Max(ClapSource.Latency, 0)));
                if (headGap > 0)
                {
                    System.Threading.Thread.Sleep(TimeSpan.FromSeconds(headGap));
                }
                if (!Playing) return;
                SoundManager.Play(context.MusicSource.FilePath, startTime + context.MusicSource.Latency);
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
            Started?.Invoke(this, EventArgs.Empty);
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
