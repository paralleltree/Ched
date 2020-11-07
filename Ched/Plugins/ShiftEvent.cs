using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

using Ched.Core;
using Ched.Core.Events;
using Ched.Localization;
using Ched.UI.Forms;

namespace Ched.Plugins
{
    public class ShiftEvent : IScorePlugin
    {
        public string DisplayName => "イベント移動";

        public void Run(IScorePluginArgs args)
        {
            var form = new ShiftTimeSelectionForm();
            if (form.ShowDialog() != DialogResult.OK) return;
            if (form.CountValue == 0) return;

            var score = args.GetCurrentScore();
            int origin = args.GetSelectedRange().StartTick;
            BarIndexCalculator barIndexCalculator;
            try
            {
                barIndexCalculator = new BarIndexCalculator(score.TicksPerBeat, score.Events.TimeSignatureChangeEvents);
            }
            catch (InvalidTimeSignatureException ex)
            {
                int beatAt = ex.Tick / score.TicksPerBeat + 1;
                MessageBox.Show(string.Format(ErrorStrings.InvalidTimeSignature, beatAt), DisplayName, MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            var barIndex = barIndexCalculator.GetBarPositionFromTick(origin).BarIndex;
            var sig = barIndexCalculator.GetTimeSignatureFromBarIndex(barIndex);
            int offset = 4 * score.TicksPerBeat * form.CountValue * (form.DurationType == DurationType.Bar ? sig.Numerator : 1) / sig.Denominator;

            var (heading, targets) = Partition(score.Events.AllEvents.Where(p => p.Tick > 0), p => p.Tick <= origin);
            if (targets.Count == 0) return;
            int firstEventTick = targets.Min(p => p.Tick);
            int lowerLimitTick = heading.Count == 0 ? 0 : heading.Max(p => p.Tick);

            if (offset < 0 && firstEventTick + offset <= lowerLimitTick)
            {
                MessageBox.Show("移動対象のイベントが先行するイベントを追い越すため、移動は実行されません。", DisplayName, MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            foreach (var item in targets) item.Tick += offset;

            args.UpdateScore(score);
        }

        protected (IList<T>, IList<T>) Partition<T>(IEnumerable<T> collection, Func<T, bool> predicate)
        {
            var trueList = new List<T>();
            var falseList = new List<T>();
            foreach (var item in collection) (predicate(item) ? trueList : falseList).Add(item);
            return (trueList, falseList);
        }

        public enum DurationType
        {
            Bar,
            Beat
        }
    }
}
