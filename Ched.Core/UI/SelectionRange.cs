using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Ched.Core;

namespace Ched.UI
{
    /// <summary>
    /// 選択範囲を表します。
    /// </summary>
    public struct SelectionRange
    {
        public static SelectionRange Empty = new SelectionRange()
        {
            StartTick = 0,
            Duration = 0,
            StartLaneIndex = 0,
            SelectedLanesCount = 0
        };

        private int startTick;
        private int duration;
        private int startLaneIndex;
        private int selectedLanesCount;

        /// <summary>
        /// 選択を開始したTickを設定します。
        /// </summary>
        public int StartTick
        {
            get { return startTick; }
            set
            {
                if (value < 0) throw new ArgumentOutOfRangeException("value must not be negative.");
                startTick = value;
            }
        }

        /// <summary>
        /// 選択を終了したときの<see cref="StartTick"/>とのオフセットを表すTickを設定します。
        /// この値が負であるとき、<see cref="StartTick"/>よりも前の範囲が選択されたことを表します。
        /// </summary>
        public int Duration
        {
            get { return duration; }
            set
            {
                duration = value;
            }
        }

        /// <summary>
        /// 選択されたレーンの左端のインデックスを設定します。
        /// </summary>
        public int StartLaneIndex
        {
            get { return startLaneIndex; }
            set
            {
                startLaneIndex = value;
            }
        }

        /// <summary>
        /// 選択されたレーン数を設定します。
        /// </summary>
        public int SelectedLanesCount
        {
            get { return selectedLanesCount; }
            set
            {
                if (StartLaneIndex + value < -32) throw new ArgumentOutOfRangeException();
                selectedLanesCount = value;
            }
        }
    }
}
