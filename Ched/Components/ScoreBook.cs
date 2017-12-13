using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Threading.Tasks;

namespace Ched.Components
{
    /// <summary>
    /// 譜面ファイルを表すクラスです。
    /// </summary>
    public class ScoreBook
    {
        /// <summary>
        /// ファイルを作成したアプリケーションのバージョンを設定します。
        /// </summary>
        public Version Version { get; set; } = System.Reflection.Assembly.GetEntryAssembly().GetName().Version;

        /// <summary>
        /// 楽曲のタイトルを設定します。
        /// </summary>
        public string Title { get; set; } = "";

        /// <summary>
        /// 楽曲のアーティストを設定します。
        /// </summary>
        public string ArtistName { get; set; } = "";

        /// <summary>
        /// 譜面制作者名を設定します。
        /// </summary>
        public string NotesDesignerName { get; set; } = "";

        /// <summary>
        /// 譜面データを設定します。
        /// </summary>
        public Score Score { get; set; } = new Score();
    }
}
