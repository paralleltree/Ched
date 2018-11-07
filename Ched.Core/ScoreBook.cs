using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.IO.Compression;
using System.Threading.Tasks;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Ched.Core
{
    /// <summary>
    /// 譜面ファイルを表すクラスです。
    /// </summary>
    [Newtonsoft.Json.JsonObject(Newtonsoft.Json.MemberSerialization.OptIn)]
    public class ScoreBook
    {
        internal static JsonSerializerSettings SerializerSettings = new JsonSerializerSettings()
        {
            ReferenceLoopHandling = ReferenceLoopHandling.Serialize,
            PreserveReferencesHandling = PreserveReferencesHandling.Objects,
            TypeNameHandling = TypeNameHandling.Auto,
            Formatting = Formatting.Indented
        };

        [Newtonsoft.Json.JsonProperty]
        private Version version = System.Reflection.Assembly.GetEntryAssembly().GetName().Version;
        [Newtonsoft.Json.JsonProperty]
        private string title = "";
        [Newtonsoft.Json.JsonProperty]
        private string artistName = "";
        [Newtonsoft.Json.JsonProperty]
        private string notesDesignerName = "";
        [Newtonsoft.Json.JsonProperty]
        private Score score = new Score();
        [Newtonsoft.Json.JsonProperty]
        private Dictionary<string, object> exporterArgs = new Dictionary<string, object>();

        public string Path { get; set; }

        /// <summary>
        /// ファイルを作成したアプリケーションのバージョンを設定します。
        /// </summary>
        public Version Version
        {
            get { return version; }
            set { version = value; }
        }

        /// <summary>
        /// 楽曲のタイトルを設定します。
        /// </summary>
        public string Title
        {
            get { return title; }
            set { title = value; }
        }

        /// <summary>
        /// 楽曲のアーティストを設定します。
        /// </summary>
        public string ArtistName
        {
            get { return artistName; }
            set { artistName = value; }
        }

        /// <summary>
        /// 譜面制作者名を設定します。
        /// </summary>
        public string NotesDesignerName
        {
            get { return notesDesignerName; }
            set { notesDesignerName = value; }
        }

        /// <summary>
        /// 譜面データを設定します。
        /// </summary>
        public Score Score
        {
            get { return score; }
            set { score = value; }
        }

        /// <summary>
        /// エクスポート用の設定を格納します。
        /// </summary>
        public Dictionary<string, object> ExporterArgs
        {
            get { return exporterArgs; }
            set { exporterArgs = value; }
        }

        public void Save(string path)
        {
            Path = path;
            Save();
        }

        public void Save()
        {
            string data = JsonConvert.SerializeObject(this, SerializerSettings);
            byte[] bytes = Encoding.UTF8.GetBytes(data);
            using (var stream = new MemoryStream(bytes))
            {
                using (var file = new FileStream(Path, FileMode.Create))
                using (var gz = new GZipStream(file, CompressionMode.Compress))
                {
                    stream.CopyTo(gz);
                }
            }
        }

        /// <summary>
        /// 指定のファイルから<see cref="ScoreBook"/>のインスタンスを生成します。
        /// 古いバージョンのファイルは現在のバージョン用に変換されます。
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        public static ScoreBook LoadFile(string path)
        {
            string data = GetDecompressedData(path);
            var doc = JObject.Parse(data);
            var fileVersion = GetFileVersion(path);

            if (fileVersion.Major < 2)
            {
                // Major = 2用の変換
                foreach (var slide in doc["score"]["notes"]["slides"])
                {
                    var obj = (JObject)slide;
                    slide["startWidth"] = obj.Property("width").Value;
                    obj.Property("width").Remove();
                }
            }

            doc["version"] = JObject.FromObject(System.Reflection.Assembly.GetEntryAssembly().GetName().Version);

            var res = doc.ToObject<ScoreBook>(JsonSerializer.Create(SerializerSettings));
            // デシリアライズ時にリストを置き換えるのではなく各要素がAddされてるようなんですが
            if (res.Score.Events.BPMChangeEvents.Count > 1)
                res.Score.Events.BPMChangeEvents = res.Score.Events.BPMChangeEvents.Skip(1).ToList();
            if (res.Score.Events.TimeSignatureChangeEvents.Count > 1)
                res.Score.Events.TimeSignatureChangeEvents = res.Score.Events.TimeSignatureChangeEvents.Skip(1).ToList();
            // ハイスピ変更イベントは元々要素ない
            // 循環参照は復元できないねん……
            foreach (var note in res.Score.Notes.AirActions)
            {
                var restored = new List<Notes.AirAction.ActionNote>(note.ActionNotes.Select(p => new Notes.AirAction.ActionNote(note) { Offset = p.Offset }));
                note.ActionNotes.Clear();
                note.ActionNotes.AddRange(restored);
            }
            res.Path = path;
            return res;
        }

        /// <summary>
        /// 指定のファイルが現在のバージョンと互換性があるかどうか調べます。
        /// </summary>
        /// <param name="path">ファイルへのパス</param>
        /// <returns>互換性があればtrue, 互換性がなければfalse</returns>
        public static bool IsCompatible(string path)
        {
            Version current = System.Reflection.Assembly.GetEntryAssembly().GetName().Version;
            return GetFileVersion(path).Major <= current.Major;
        }

        /// <summary>
        /// 指定のファイルに対してバージョンアップが必要かどうか調べます。
        /// </summary>
        /// <param name="path">ファイルへのパス</param>
        /// <returns>読み込み可能であればtrue, 不可能であればfalse</returns>
        public static bool IsUpgradeNeeded(string path)
        {
            Version current = System.Reflection.Assembly.GetEntryAssembly().GetName().Version;
            return GetFileVersion(path).Major == current.Major;
        }

        private static string GetDecompressedData(string path)
        {
            using (var gz = new GZipStream(new FileStream(path, FileMode.Open), CompressionMode.Decompress))
            using (var stream = new MemoryStream())
            {
                gz.CopyTo(stream);
                return Encoding.UTF8.GetString(stream.ToArray());
            }
        }

        /// <summary>
        /// 指定のファイルに保存されたデータのバージョンを取得します。
        /// </summary>
        /// <param name="path">ファイルへのパス</param>
        /// <returns>ファイルが生成されたバージョン</returns>
        private static Version GetFileVersion(string path)
        {
            var doc = JObject.Parse(GetDecompressedData(path));
            Version current = System.Reflection.Assembly.GetEntryAssembly().GetName().Version;
            return doc["version"].ToObject<Version>();
        }
    }
}
