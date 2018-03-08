using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using SharpDX.IO;
using SharpDX.MediaFoundation;
using SharpDX.XAudio2;

namespace Ched.UI
{
    public class SoundManager : IDisposable
    {
        private readonly XAudio2 xAudio = new XAudio2();
        readonly HashSet<SourceVoice> playing = new HashSet<SourceVoice>();
        readonly Dictionary<string, AudioDecoder> sounds = new Dictionary<string, AudioDecoder>();
        readonly Dictionary<AudioDecoder, Queue<SourceVoice>> voices = new Dictionary<AudioDecoder, Queue<SourceVoice>>();

        public void Dispose()
        {
            foreach (var item in voices) item.Key.Dispose();
            xAudio.Dispose();
        }

        public SoundManager()
        {
            xAudio.StartEngine();
            new MasteringVoice(xAudio).SetVolume(1);
        }

        public void Register(string filepath)
        {
            if (sounds.ContainsKey(filepath)) return;
            var nfs = new NativeFileStream(filepath, NativeFileMode.Open, NativeFileAccess.Read);
            var decoder = new AudioDecoder(nfs);
            lock (sounds) sounds.Add(filepath, decoder);

            var freelist = new Queue<SourceVoice>();
            voices.Add(decoder, freelist);
        }

        public void Play(string path)
        {
            Play(path, TimeSpan.Zero);
        }

        public void Play(string path, TimeSpan offset)
        {
            Task.Run(() => PlayInternal(path, offset));
        }

        private void PlayInternal(string path, TimeSpan offset)
        {
            AudioDecoder decoder;
            Queue<SourceVoice> freelist;

            Register(path);

            lock (sounds)
            {
                decoder = sounds[path];
                freelist = voices[decoder];
            }


            SourceVoice voice;
            lock (freelist)
            {
                if (freelist.Count > 0)
                {
                    voice = freelist.Dequeue();
                }
                else
                {
                    voice = new SourceVoice(xAudio, decoder.WaveFormat);
                }
            }

            var it = decoder.GetSamples(offset).GetEnumerator();

            Action<IntPtr> lambda = null;
            lambda = i =>
            {
                if (it.MoveNext())
                {
                    voice.SubmitSourceBuffer(new AudioBuffer(it.Current), null);
                }
                else
                {
                    voice.BufferEnd -= lambda;
                    lock (freelist) freelist.Enqueue(voice);
                    lock (playing) playing.Remove(voice);
                }
            };
            voice.BufferEnd += lambda;

            if (it.MoveNext()) voice.SubmitSourceBuffer(new AudioBuffer(it.Current), null);
            voice.Start();
            lock (playing) playing.Add(voice);
        }

        public void StopAll()
        {
            lock (playing)
            {
                foreach (var voice in playing)
                {
                    voice.Stop();
                }
                playing.Clear();
            }
        }
    }

    /// <summary>
    /// 音源を表すクラスです。
    /// </summary>
    [Serializable]
    public class SoundSource
    {
        /// <summary>
        /// この音源における遅延時間を取得します。
        /// この値は、タイミングよく音声が出力されるまでの秒数です。
        /// </summary>
        public double Latency { get; }

        public string FilePath { get; }

        public SoundSource(string path, double latency)
        {
            FilePath = path;
            Latency = latency;
        }
    }
}
