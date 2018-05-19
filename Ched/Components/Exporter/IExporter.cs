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
    public interface IExporter
    {
        /// <summary>
        /// フォーマット名を取得します。
        /// </summary>
        string FormatName { get; }

        /// <summary>
        /// 指定のファイルへデータをエクスポートします。
        /// </summary>
        /// <param name="path">エクスポート先のパス</param>
        /// <param name="book">譜面データ</param>
        void Export(string path, ScoreBook book);
    }

    /// <summary>
    /// 独自の拡張情報を持つエクスポート可能な形式を表すインターフェースです。
    /// </summary>
    /// <typeparam name="TArgs">拡張情報用クラス</typeparam>
    public interface IExtendedExpoerter<TArgs> : IExporter
    {
        TArgs CustomArgs { get; set; }
    }
}
