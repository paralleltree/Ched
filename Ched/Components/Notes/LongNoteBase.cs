using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ched.Components.Notes
{
    public interface ILongNote
    {
        int StartTick { get; }
        int GetDuration();
    }

    public abstract class LongNoteBase : NoteBase, ILongNote
    {
        /// <summary>
        /// ノートの開始位置を表すTickを設定します。
        /// </summary>
        public abstract int StartTick { get; }

        /// <summary>
        /// ノートの長さを表すTickを取得します。
        /// </summary>
        public abstract int GetDuration();
    }

    [Newtonsoft.Json.JsonObject(Newtonsoft.Json.MemberSerialization.OptIn)]
    public abstract class MovableLongNoteBase : NoteBase, ILongNote
    {
        [Newtonsoft.Json.JsonProperty]
        private int startTick;

        /// <summary>
        /// ノートの開始位置を表すTickを設定します。
        /// </summary>
        public int StartTick
        {
            get { return startTick; }
            set
            {
                if (startTick == value) return;
                if (value < 0) throw new ArgumentOutOfRangeException("value", "value must not be negative.");
                startTick = value;
            }
        }

        /// <summary>
        /// ノートの長さを表すTickを取得します。
        /// </summary>
        public abstract int GetDuration();
    }

    public abstract class LongNoteTapBase : TapBase, IAirable
    {
        private static readonly Color AirDarkNoteColor = Color.FromArgb(6, 180, 10);
        private static readonly Color AirLightNoteColor = Color.FromArgb(80, 224, 64);

        public abstract bool IsTap { get; }
        public abstract int LaneIndex { get; }
        public abstract int Tick { get; }
        public abstract int Width { get; }

        internal override void Draw(Graphics g, RectangleF rect)
        {
            Draw(g, rect, false);
        }

        internal void Draw(Graphics g, RectangleF rect, bool isAir)
        {
            if (isAir)
            {
                DrawNote(g, rect, AirDarkNoteColor, AirLightNoteColor);
                DrawBorder(g, rect);
            }
            else
            {
                base.Draw(g, rect);
                if (IsTap) DrawTapSymbol(g, rect);
            }
        }
    }

    public interface ITapSelectable
    {
        bool IsTap { get; set; }
    }
}
