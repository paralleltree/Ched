using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Ched.Core;
using Ched.Core.Events;
using Ched.UI;

namespace Ched.Plugins
{
    public class ScorePluginArgs : IScorePluginArgs
    {
        private Func<Score> getScoreFunc;
        private SelectionRange selectedRange;
        private Action<Score> updateScoreAction;

        public ScorePluginArgs(Func<Score> getScoreFunc, SelectionRange selectedRange, Action<Score> updateScoreAction)
        {
            this.getScoreFunc = getScoreFunc;
            this.selectedRange = selectedRange;
            this.updateScoreAction = updateScoreAction;
        }

        public Score GetCurrentScore() => getScoreFunc();

        public SelectionRange GetSelectedRange() => selectedRange;

        public void UpdateScore(Score score)
        {
            CheckEventDuplicate(score.Events.BpmChangeEvents);
            CheckEventDuplicate(score.Events.TimeSignatureChangeEvents);
            CheckEventDuplicate(score.Events.HighSpeedChangeEvents);
            updateScoreAction(score);
        }

        private void CheckEventDuplicate<T>(IList<T> src) where T : EventBase
        {
            var set = new HashSet<int>();
            if (src.All(p => set.Add(p.Tick))) return;
            throw new ArgumentException("There are some events in same ticks.");
        }
    }
}
