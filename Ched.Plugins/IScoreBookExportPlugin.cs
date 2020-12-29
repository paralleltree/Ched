using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Ched.Core;

namespace Ched.Plugins
{
    /// <summary>
    /// 譜面データのエクスポートを行うプラグインを表します。
    /// </summary>
    public interface IScoreBookExportPlugin : IPlugin
    {
        /// <summary>
        /// ファイル選択ダイアログで利用するフィルタ文字列を取得します。
        /// </summary>
        string FileFilter { get; }

        /// <summary>
        /// エクスポート処理を実行します。
        /// </summary>
        /// <param name="args">エクスポート時の情報を取得する<see cref="IScoreBookExportPluginArgs"/></param>
        /// <remarks>メソッドの呼び出し後に処理をキャンセルする場合、<see cref="UserCancelledException"/>をスローします。</remarks>
        void Export(IScoreBookExportPluginArgs args);
    }

    /// <summary>
    /// エクスポート時にプラグインへ渡される情報を表します。
    /// </summary>
    public interface IScoreBookExportPluginArgs : IDiagnosable
    {
        /// <summary>
        /// データを書き込む<see cref="System.IO.Stream"/>を取得します。
        /// </summary>
        Stream Stream { get; }

        /// <summary>
        /// 追加情報を入力する必要があるかどうかを示す値を取得します。
        /// この値がTrueの場合、追加情報を要求するダイアログを表示する必要はありません。
        /// </summary>
        bool IsQuick { get; }

        /// <summary>
        /// エクスポートするデータを取得します。
        /// </summary>
        /// <returns>エクスポートする譜面データを表す<see cref="ScoreBook"/></returns>
        ScoreBook GetScoreBook();

        /// <summary>
        /// エクスポートされるデータに関連付けられた追加情報を取得します。
        /// </summary>
        /// <returns>追加情報を表す文字列</returns>
        string GetCustomData();

        /// <summary>
        /// エクスポートしたデータに関連付ける追加情報を保存します。
        /// </summary>
        /// <param name="data">保存する追加情報を表す文字列</param>
        void SetCustomData(string data);
    }
}
