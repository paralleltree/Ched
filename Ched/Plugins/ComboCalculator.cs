using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

using Ched.Core;
using Ched.Core.Notes;

namespace Ched.Plugins
{
    public class ComboCalculator : IScorePlugin
    {
        public string DisplayName => "コンボ計算";

        public void Run(IScorePluginArgs args)
        {
            var score = args.GetCurrentScore();
            var combo = CalculateCombo(score);
            var sb = new StringBuilder();
            sb.AppendLine(string.Format("総コンボ数: {0}", combo.Total));
            sb.AppendLine(string.Format("TAP: {0}", combo.Tap));
            sb.AppendLine(string.Format("HOLD: {0}", combo.Hold));
            sb.AppendLine(string.Format("SLIDE: {0}", combo.Slide));
            sb.AppendLine(string.Format("AIR: {0}", combo.Air));
            sb.AppendLine(string.Format("FLICK: {0}", combo.Flick));

            MessageBox.Show(sb.ToString(), DisplayName);
        }

        protected ComboDetails CalculateCombo(Score score)
        {
            var combo = new ComboDetails();
            combo.Tap += new int[]
            {
                score.Notes.Taps.Count,
                score.Notes.ExTaps.Count,
                score.Notes.Damages.Count,
                score.Notes.Holds.Count,
                score.Notes.Slides.Count
            }.Sum();

            combo.Flick += score.Notes.Flicks.Count;
            combo.Air += score.Notes.Airs.Count;

            int barTick = 4 * score.TicksPerBeat;
            var bpmEvents = score.Events.BPMChangeEvents.OrderBy(p => p.Tick).ToList();
            var airList = new HashSet<IAirable>(score.Notes.Airs.Select(p => p.ParentNote));
            Func<int, decimal> getHeadBpmAt = tick => (bpmEvents.LastOrDefault(p => p.Tick <= tick) ?? bpmEvents[0]).BPM;
            Func<int, decimal> getTailBpmAt = tick => (bpmEvents.LastOrDefault(p => p.Tick < tick) ?? bpmEvents[0]).BPM;
            Func<decimal, int> comboDivider = bpm => bpm < 120 ? 16 : (bpm < 240 ? 8 : 4);

            // コンボとしてカウントされるstartTickからのオフセットを求める
            Func<int, IEnumerable<int>, List<int>> calcComboTicks = (startTick, stepTicks) =>
            {
                var tickList = new List<int>();
                var sortedStepTicks = stepTicks.OrderBy(p => p).ToList();
                int duration = sortedStepTicks[sortedStepTicks.Count - 1];
                int head = 0;
                int bpmIndex = 0;
                int stepIndex = 0;

                while (head < duration)
                {
                    while (bpmIndex + 1 < bpmEvents.Count && startTick + head >= bpmEvents[bpmIndex + 1].Tick) bpmIndex++;
                    int interval = barTick / comboDivider(bpmEvents[bpmIndex].BPM);
                    int diff = Math.Min(interval, sortedStepTicks[stepIndex] - head);
                    head += diff;
                    tickList.Add(head);
                    if (head == sortedStepTicks[stepIndex]) stepIndex++;
                }

                return tickList;
            };
            Func<IEnumerable<int>, int, int, IEnumerable<int>> removeLostTicks = (ticks, startTick, duration) =>
            {
                int interval = barTick / comboDivider(getTailBpmAt(startTick + duration));
                return ticks.Where(p => p <= duration - interval).ToList();
            };

            foreach (var hold in score.Notes.Holds)
            {
                var tickList = new HashSet<int>(calcComboTicks(hold.StartTick, new int[] { hold.Duration }));

                if (airList.Contains(hold.EndNote))
                {
                    combo.Hold += removeLostTicks(tickList, hold.StartTick, hold.Duration).Count();
                }
                else
                {
                    combo.Hold += tickList.Count;
                }
            }

            foreach (var slide in score.Notes.Slides)
            {
                var tickList = new HashSet<int>(calcComboTicks(slide.StartTick, slide.StepNotes.Where(p => p.IsVisible).Select(p => p.TickOffset)));

                if (airList.Contains(slide.StepNotes.OrderByDescending(p => p.TickOffset).First()))
                {
                    combo.Slide += removeLostTicks(tickList, slide.StartTick, slide.GetDuration()).Count();
                }
                else
                {
                    combo.Slide += tickList.Count;
                }
            }

            foreach (var airAction in score.Notes.AirActions)
            {
                var lostSections = airAction.ActionNotes.Select(p => p.Offset).Concat(new[] { 0 }).Select(p =>
                {
                    int interval = barTick / comboDivider(getHeadBpmAt(airAction.StartTick + p));
                    return Tuple.Create(p, interval);
                }).ToList();

                var validTicks = calcComboTicks(airAction.StartTick, airAction.ActionNotes.Select(p => p.Offset))
                    .Where(p => lostSections.All(q => p <= q.Item1 || p > q.Item1 + q.Item2));

                combo.Air += new HashSet<int>(validTicks).Count;
            }

            return combo;
        }

        public struct ComboDetails
        {
            public int Total => Tap + Hold + Slide + Air + Flick;
            public int Tap { get; set; }
            public int Hold { get; set; }
            public int Slide { get; set; }
            public int Air { get; set; }
            public int Flick { get; set; }
        }
    }
}
