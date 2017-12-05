using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ched.UI.Operations
{
    /// <summary>
    /// 複数の<see cref="IOperation"/>からなる1つの操作を表します。
    /// </summary>
    internal class CompositeOperation : IOperation
    {
        public string Description { get; }

        protected IEnumerable<IOperation> Operations { get; }

        /// <summary>
        /// 操作の説明と<see cref="IEnumerable{IOperation}"/>からこの<see cref="CompositeOperation"/>を初期化します。
        /// </summary>
        /// <param name="description">この操作の説明</param>
        /// <param name="operations">操作順にソートされた<see cref="IEnumerable{IOperation}"/></param>
        public CompositeOperation(string description, IEnumerable<IOperation> operations)
        {
            Description = description;
            Operations = operations;
        }

        public void Redo()
        {
            foreach (var op in Operations) op.Redo();
        }

        public void Undo()
        {
            foreach (var op in Operations.Reverse()) op.Undo();
        }
    }
}
