using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Un4seen.Bass;

namespace Ched.UI
{
    public class SoundManager : IDisposable
    {
        readonly HashSet<int> playing = new HashSet<int>();
        readonly HashSet<SYNCPROC> syncProcs = new HashSet<SYNCPROC>();
        readonly Dictionary<string, Queue<int>> handles = new Dictionary<string, Queue<int>>();
        readonly Dictionary<string, double> durations = new Dictionary<string, double>();

        public bool IsSupported { get; private set; } = true;

        public event EventHandler ExceptionThrown;

        public void Dispose()
        {
            if (!IsSupported) return;
            Bass.BASS_Stop();
            Bass.BASS_PluginFree(0);
            Bass.BASS_Free();
        }

        public SoundManager()
        {
            // なぜBass.LoadMe()呼び出すとfalseなんでしょうね
            if (!Bass.BASS_Init(-1, 44100, BASSInit.BASS_DEVICE_DEFAULT, IntPtr.Zero))
            {
                IsSupported = false;
                return;
            }
        }

        public void Register(string path)
        {
            CheckSupported();
            lock (handles)
            {
                if (handles.ContainsKey(path)) return;
                int handle = GetHandle(path);
                long len = Bass.BASS_ChannelGetLength(handle);
                handles.Add(path, new Queue<int>());
                lock (durations) durations.Add(path, Bass.BASS_ChannelBytes2Seconds(handle, len));
            }
        }

        protected int GetHandle(string filepath)
        {
            int handle = Bass.BASS_StreamCreateFile(filepath, 0, 0, BASSFlag.BASS_DEFAULT);
            if (handle == 0) throw new ArgumentException("cannot create a stream.");
            return handle;
        }

        public void Play(string path)
        {
            Play(path, 0, 1.0);
        }

        public void Play(string path, double offset, double volume)
        {
            CheckSupported();
            Task.Run(() => PlayInternal(path, offset, volume))
                .ContinueWith(p =>
                {
                    if (p.Exception != null)
                    {
                        Program.DumpExceptionTo(p.Exception, "sound_exception.json");
                        ExceptionThrown?.Invoke(this, EventArgs.Empty);
                    }
                });
        }

        private void PlayInternal(string path, double offset, double volume)
        {
            Queue<int> freelist;
            lock (handles)
            {
                if (!handles.ContainsKey(path)) throw new InvalidOperationException("sound source was not registered.");
                freelist = handles[path];
            }

            int handle;
            lock (freelist)
            {
                if (freelist.Count > 0) handle = freelist.Dequeue();
                else
                {
                    handle = GetHandle(path);

                    var proc = new SYNCPROC((h, channel, data, user) =>
                    {
                        lock (freelist) freelist.Enqueue(handle);
                    });

                    int syncHandle;
                    syncHandle = Bass.BASS_ChannelSetSync(handle, BASSSync.BASS_SYNC_END, 0, proc, IntPtr.Zero);
                    if (syncHandle == 0) throw new InvalidOperationException("cannot set sync");
                    lock (syncProcs) syncProcs.Add(proc); // avoid GC
                }
            }

            lock (playing) playing.Add(handle);
            Bass.BASS_ChannelSetPosition(handle, offset);
            Bass.BASS_ChannelSetAttribute(handle, BASSAttribute.BASS_ATTRIB_VOL, (float)volume);
            Bass.BASS_ChannelPlay(handle, false);
        }

        public void StopAll()
        {
            CheckSupported();
            lock (playing)
            {
                foreach (int handle in playing)
                {
                    Bass.BASS_ChannelStop(handle);
                }
                playing.Clear();
            }
        }

        public double GetDuration(string path)
        {
            Register(path);
            lock (durations) return durations[path];
        }

        protected void CheckSupported()
        {
            if (IsSupported) return;
            throw new NotSupportedException("The sound engine is not supported.");
        }
    }

    /// <summary>
    /// 音源を表すクラスです。
    /// </summary>
    [Serializable]
    public class SoundSource
    {
        public static readonly IReadOnlyCollection<string> SupportedExtensions = new string[] { ".wav", ".mp3", ".ogg" };

        /// <summary>
        /// この音源における遅延時間を取得します。
        /// この値は、タイミングよく音声が出力されるまでの秒数です。
        /// </summary>
        public double Latency { get; set; }

        public string FilePath { get; set; }

        private double volume = 1.0;
        public double Volume
        {
            get => volume;
            set
            {
                if (volume < 0 || volume > 1.0)
                    throw new ArgumentOutOfRangeException("value");
                volume = value;
            }
        }

        public SoundSource()
        {
        }

        public SoundSource(string path, double latency)
        {
            FilePath = path;
            Latency = latency;
        }
    }
}
