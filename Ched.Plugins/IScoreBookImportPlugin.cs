using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Text;
using System.Threading.Tasks;

using Ched.Core;

namespace Ched.Plugins
{
    /// <summary>
    /// 譜面データのインポートを行うプラグインを表します。
    /// </summary>
    public interface IScoreBookImportPlugin : IPlugin
    {
        /// <summary>
        /// ファイル選択時に利用するフィルタ文字列を取得します。
        /// </summary>
        string FileFilter { get; }

        /// <summary>
        /// 譜面データのインポート処理を行います。
        /// </summary>
        /// <param name="args">インポート時に渡される情報を表す<see cref="IScoreBookImportPluginArgs"/></param>
        /// <returns>インポートされる譜面を表す<see cref="ScoreBook"/></returns>
        ScoreBook Import(IScoreBookImportPluginArgs args);
    }

    /// <summary>
    /// 譜面データのインポート時に渡される情報を表します。
    /// </summary>
    public interface IScoreBookImportPluginArgs : IDiagnosable
    {
        /// <summary>
        /// データを読み取るストリームを取得します。
        /// </summary>
        Stream Stream { get; }
    }
}
