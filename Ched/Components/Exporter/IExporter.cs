using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Ched.Components;

namespace Ched.Components.Exporter
{
    /// <summary>
    /// エクスポート可能な形式を表すインターフェースです。
    /// </summary>
    /// <typeparam name="TArgs"></typeparam>
    public interface IExporter<TArgs>
    {
        /// <summary>
        /// フォーマット名を取得します。
        /// </summary>
        string FormatName { get; }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="path">エクスポート先のパス</param>
        /// <param name="book">譜面データ</param>
        /// <param name="args">エクスポート用の拡張情報</param>
        void Export(string path, ScoreBook book, TArgs args);
    }
}
