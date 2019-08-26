using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Ched.Core;

namespace Ched.UI.Operations
{
    public class UpdateScoreOperation : IOperation
    {
        public string Description { get { return "譜面の更新"; } }

        protected Action<Score> setScoreAction;
        protected Score BeforeScore { get; }
        protected Score AfterScore { get; }

        public UpdateScoreOperation(Score beforeScore, Score afterScore, Action<Score> setScoreAction)
        {
            BeforeScore = beforeScore;
            AfterScore = afterScore;
            this.setScoreAction = setScoreAction;
        }

        public void Undo()
        {
            setScoreAction(BeforeScore);
        }

        public void Redo()
        {
            setScoreAction(AfterScore);
        }
    }
}
