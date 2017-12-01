using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Ched.UI
{
    public partial class NoteView : Control
    {
        public event EventHandler NoteChanged;

        /// <summary>
        /// 小節の区切り線の色を設定します。
        /// </summary>
        public Color BarLineColor { get; set; } = Color.FromArgb(80, 80, 80);

        /// <summary>
        /// 1拍のガイド線の色を設定します。
        /// </summary>
        public Color BeatLineColor { get; set; } = Color.FromArgb(120, 120, 120);

        /// <summary>
        /// レーンのガイド線の色を設定します。
        /// </summary>
        public Color LaneBorderColor { get; set; } = Color.FromArgb(208, 208, 208);

        /// <summary>
        /// 1拍あたりのTick数を取得します。
        /// </summary>
        public int UnitBeatTick { get { return 480; } }

        /// <summary>
        /// 1拍あたりの表示高さを設定します。
        /// </summary>
        public int UnitBeatHeight { get; set; }

        /// <summary>
        /// 表示始端のTickを設定します。
        /// </summary>
        public int HeadTick { get; set; }

        public NoteView()
        {
            InitializeComponent();
            this.DoubleBuffered = true;
        }

        protected override void OnPaint(PaintEventArgs pe)
        {
            base.OnPaint(pe);
        }
    }

    public enum NotesType
    {
        Tap,
        ExTap,
        Hold,
        Air,
        AirAction,
        Flick,
        Damage
    }
}
