using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ched.UI.Operations
{
    /// <summary>
    /// 操作を管理するクラスです。
    /// </summary>
    public class OperationManager
    {
        public event EventHandler OperationHistoryChanged;
        public event EventHandler ChangesCommitted;

        protected Stack<IOperation> UndoStack { get; } = new Stack<IOperation>();
        protected Stack<IOperation> RedoStack { get; } = new Stack<IOperation>();

        private IOperation LastCommittedOperation { get; set; } = null;

        /// <summary>
        /// 元に戻す操作の概要のコレクションを取得します。
        /// </summary>
        public IEnumerable<string> UndoOperationsDescription
        {
            get { return UndoStack.Select(p => p.Description); }
        }

        /// <summary>
        /// やり直す操作の概要のコレクションを取得します。
        /// </summary>
        public IEnumerable<string> RedoOperationsDescription
        {
            get { return RedoStack.Select(p => p.Description); }
        }

        /// <summary>
        /// 操作を元に戻せるかどうかを取得します。
        /// </summary>
        public bool CanUndo { get { return UndoStack.Count > 0; } }

        /// <summary>
        /// 操作をやり直せるかどうかを取得します。
        /// </summary>
        public bool CanRedo { get { return RedoStack.Count > 0; } }

        /// <summary>
        /// 前回の<see cref="CommitChanges"/>の呼び出しから変更が加えられているかどうかを取得します。
        /// </summary>
        public bool IsChanged { get { return LastCommittedOperation != (UndoStack.Count > 0 ? UndoStack.Peek() : null); } }

        /// <summary>
        /// 新たな操作を記録します。
        /// </summary>
        /// <param name="op">記録する操作</param>
        public void Push(IOperation op)
        {
            UndoStack.Push(op);
            RedoStack.Clear();
            OperationHistoryChanged?.Invoke(this, EventArgs.Empty);
        }

        /// <summary>
        /// 操作を実行し、記録します。
        /// </summary>
        /// <param name="op">実行・記録する操作</param>
        public void InvokeAndPush(IOperation op)
        {
            op.Redo();
            Push(op);
        }

        /// <summary>
        /// 直前の操作を元に戻します。
        /// </summary>
        public void Undo()
        {
            IOperation op = UndoStack.Pop();
            op.Undo();
            RedoStack.Push(op);
            OperationHistoryChanged?.Invoke(this, EventArgs.Empty);
        }

        /// <summary>
        /// 直後の操作をやり直します。
        /// </summary>
        public void Redo()
        {
            IOperation op = RedoStack.Pop();
            op.Redo();
            UndoStack.Push(op);
            OperationHistoryChanged?.Invoke(this, EventArgs.Empty);
        }

        /// <summary>
        /// 記録されている操作をクリアします。
        /// </summary>
        public void Clear()
        {
            UndoStack.Clear();
            RedoStack.Clear();
            LastCommittedOperation = null;
            OperationHistoryChanged?.Invoke(this, EventArgs.Empty);
        }

        /// <summary>
        /// 現在の<see cref="OperationManager"/>の状態に対して、保存処理が行われたことを通知します。
        /// </summary>
        public void CommitChanges()
        {
            LastCommittedOperation = UndoStack.Count > 0 ? UndoStack.Peek() : null;
            ChangesCommitted?.Invoke(this, EventArgs.Empty);
        }
    }
}
