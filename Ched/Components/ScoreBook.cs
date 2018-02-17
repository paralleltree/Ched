﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.IO.Compression;
using System.Threading.Tasks;

using Newtonsoft.Json;

namespace Ched.Components
{
    /// <summary>
    /// 譜面ファイルを表すクラスです。
    /// </summary>
    [Newtonsoft.Json.JsonObject(Newtonsoft.Json.MemberSerialization.OptIn)]
    public class ScoreBook
    {
        private static JsonSerializerSettings SerializerSettings = new JsonSerializerSettings()
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

        public static ScoreBook LoadFile(string path)
        {
            using (var file = new FileStream(path, FileMode.Open))
            using (var gz = new GZipStream(file, CompressionMode.Decompress))
            {
                using (var stream = new MemoryStream())
                {
                    gz.CopyTo(stream);
                    string data = Encoding.UTF8.GetString(stream.ToArray());
                    var res = JsonConvert.DeserializeObject<ScoreBook>(data, SerializerSettings);
                    // デシリアライズ時にリストを置き換えるのではなく各要素がAddされてるようなんですが
                    if (res.Score.Events.BPMChangeEvents.Count > 1)
                        res.Score.Events.BPMChangeEvents = res.Score.Events.BPMChangeEvents.Skip(1).ToList();
                    if (res.score.Events.HighSpeedChangeEvents.Count > 1)
                        res.score.Events.HighSpeedChangeEvents = res.Score.Events.HighSpeedChangeEvents.Skip(1).ToList();
                    if (res.Score.Events.TimeSignatureChangeEvents.Count > 1)
                        res.Score.Events.TimeSignatureChangeEvents = res.Score.Events.TimeSignatureChangeEvents.Skip(1).ToList();
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
            }
        }
    }
}
