using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Ched.Core;

namespace Ched.Plugins
{
    public class ScorePluginArgs : IScorePluginArgs
    {
        private Func<Score> getScoreFunc;
        private Action<Score> updateScoreAction;

        public ScorePluginArgs(Func<Score> getScoreFunc, Action<Score> updateScoreAction)
        {
            this.getScoreFunc = getScoreFunc;
            this.updateScoreAction = updateScoreAction;
        }

        public Score GetCurrentScore()
        {
            return getScoreFunc();
        }

        public void UpdateScore(Score score)
        {
            updateScoreAction(score);
        }
    }
}
