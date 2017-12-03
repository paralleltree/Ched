using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ched.UI.Operations
{
    /// <summary>
    /// ユーザーの操作を表すインタフェースです。
    /// </summary>
    internal interface IOperation
    {
        /// <summary>
        /// この操作の説明を取得します。
        /// </summary>
        string Description { get; }

        /// <summary>
        /// 操作を元に戻します。
        /// </summary>
        void Undo();

        /// <summary>
        /// 操作をやり直します。
        /// </summary>
        void Redo();
    }
}
