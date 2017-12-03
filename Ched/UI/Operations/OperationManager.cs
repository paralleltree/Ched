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
    internal class OperationManager
    {
        protected Stack<IOperation> UndoStack { get; } = new Stack<IOperation>();
        protected Stack<IOperation> RedoStack { get; } = new Stack<IOperation>();

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
        /// 新たな操作を記録します。
        /// </summary>
        /// <param name="op">記録する操作</param>
        public void Push(IOperation op)
        {
            UndoStack.Push(op);
            RedoStack.Clear();
        }

        /// <summary>
        /// 直前の操作を元に戻します。
        /// </summary>
        public void Undo()
        {
            IOperation op = UndoStack.Pop();
            op.Undo();
            RedoStack.Push(op);
        }

        /// <summary>
        /// 直後の操作をやり直します。
        /// </summary>
        public void Redo()
        {
            IOperation op = RedoStack.Pop();
            op.Redo();
            UndoStack.Push(op);
        }
    }
}
