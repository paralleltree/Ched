using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Ched.Core.Events;

namespace Ched.Core
{
    /// <summary>
    /// 拍子変更イベントからTick値に対応する小節位置を求めるクラスです。
    /// </summary>
    public class BarIndexCalculator
    {
        private int TicksPerBeat { get; }
        private int BarTick => TicksPerBeat * 4;
        private IReadOnlyCollection<TimeSignatureItem> ReversedTimeSignatures { get; }

        /// <summary>
        /// 時間順にソートされた有効な拍子変更イベントのコレクションを取得します。
        /// </summary>
        public IEnumerable<TimeSignatureItem> TimeSignatures => ReversedTimeSignatures.Reverse();

        /// <summary>
        /// TicksPerBeatと拍子変更イベントから<see cref="BarIndexCalculator"/>のインスタンスを初期化します。
        /// </summary>
        /// <param name="ticksPerBeat">譜面のTicksPerBeat</param>
        /// <param name="sigs">拍子変更イベントを表す<see cref="TimeSignatureChangeEvent"/>のリスト</param>
        public BarIndexCalculator(int ticksPerBeat, IEnumerable<TimeSignatureChangeEvent> sigs)
        {
            TicksPerBeat = ticksPerBeat;
            var ordered = sigs.OrderBy(p => p.Tick).ToList();
            var dic = new SortedDictionary<int, TimeSignatureItem>();
            int pos = 0;
            int barIndex = 0;

            for (int i = 0; i < ordered.Count; i++)
            {
                // 小節先頭に配置されていないイベント
                if (pos != ordered[i].Tick) throw new InvalidTimeSignatureException($"TimeSignatureChangeEvent does not align at the head of bars (Tick: {ordered[i].Tick}).", ordered[i].Tick);
                var item = new TimeSignatureItem(barIndex, ordered[i]);

                // 時間逆順で追加
                if (dic.ContainsKey(-pos)) throw new InvalidTimeSignatureException($"TimeSignatureChangeEvents duplicated (Tick: {ordered[i].Tick}).", ordered[i].Tick);
                else dic.Add(-pos, item);

                if (i < ordered.Count - 1)
                {
                    int barLength = BarTick * ordered[i].Numerator / ordered[i].Denominator;
                    int duration = ordered[i + 1].Tick - pos;
                    pos += duration / barLength * barLength;
                    barIndex += duration / barLength;
                }
            }

            ReversedTimeSignatures = dic.Values.ToList();
        }

        /// <summary>
        /// 指定のTickに対応する小節位置を取得します。
        /// </summary>
        /// <param name="tick">小節位置を取得するTick</param>
        /// <returns>Tickに対応する小節位置を表す<see cref="BarPosition"/></returns>
        public BarPosition GetBarPositionFromTick(int tick)
        {
            foreach (var item in ReversedTimeSignatures)
            {
                if (tick < item.StartTick) continue;
                var sig = item.TimeSignature;
                int barLength = BarTick * sig.Numerator / sig.Denominator;
                int ticksFromSignature = tick - item.StartTick;
                int barsCount = ticksFromSignature / barLength;
                int barIndex = item.StartBarIndex + barsCount;
                int tickOffset = ticksFromSignature - barsCount * barLength;
                return new BarPosition(barIndex, tickOffset);
            }

            throw new InvalidOperationException();
        }

        /// <summary>
        /// 指定の小節に対応する拍子を取得します。
        /// </summary>
        /// <param name="barIndex">拍子を求める小節位置。このパラメータは0-basedです。</param>
        /// <returns>小節位置に対応する拍子を表す<see cref="TimeSignatureChangeEvent"/></returns>
        public TimeSignatureChangeEvent GetTimeSignatureFromBarIndex(int barIndex)
        {
            foreach (var item in ReversedTimeSignatures)
            {
                if (barIndex < item.StartBarIndex) continue;
                return item.TimeSignature;
            }

            throw new InvalidOperationException();
        }

        /// <summary>
        /// Tickに対応する小節位置を表します。
        /// </summary>
        public class BarPosition
        {
            /// <summary>
            /// 小節のインデックスを取得します。このフィールドは0-basedです。
            /// </summary>
            public int BarIndex { get; }

            /// <summary>
            /// 小節におけるTickのオフセットを表します。
            /// </summary>
            public int TickOffset { get; }

            public BarPosition(int barIndex, int tickOffset)
            {
                BarIndex = barIndex;
                TickOffset = tickOffset;
            }
        }

        /// <summary>
        /// 拍子変更イベントに対応するTick位置と小節位置を表すクラスです。
        /// </summary>
        public class TimeSignatureItem
        {
            /// <summary>
            /// 拍子変更イベントに対応するTick位置を取得します。
            /// </summary>
            public int StartTick => TimeSignature.Tick;

            /// <summary>
            /// 拍子変更イベントに対応する小節位置を取得します。このフィールドは0-basedです。
            /// </summary>
            public int StartBarIndex { get; }

            /// <summary>
            /// この<see cref="TimeSignatureItem"/>に関連付けられた拍子変更イベントを取得します。
            /// </summary>
            public TimeSignatureChangeEvent TimeSignature { get; }

            public TimeSignatureItem(int startBarIndex, TimeSignatureChangeEvent timeSignature)
            {
                StartBarIndex = startBarIndex;
                TimeSignature = timeSignature;
            }
        }
    }
}
