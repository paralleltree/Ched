using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Ched.Components;

namespace Ched.UI.Operations
{
    public class InsertTicksOperation : IOperation
    {
        protected Score Score { get; }
        protected int Position { get; }
        protected int Duration { get; }
        public string Description { get { return "時間挿入"; } }

        public InsertTicksOperation(Score score, int pos, int duration)
        {
            Score = score;
            Position = pos;
            Duration = duration;
        }

        public void Undo()
        {
            Score.InsertTicks(Position, -Duration);
        }

        public void Redo()
        {
            Score.InsertTicks(Position, Duration);
        }
    }
}
