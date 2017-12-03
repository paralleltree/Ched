using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

using Ched.Components;

namespace Ched.UI
{
    public partial class NoteView : Control
    {
        /// <summary>
        /// 小節の区切り線の色を設定します。
        /// </summary>
        public Color BarLineColor { get; set; } = Color.FromArgb(160, 160, 160);

        /// <summary>
        /// 1拍のガイド線の色を設定します。
        /// </summary>
        public Color BeatLineColor { get; set; } = Color.FromArgb(80, 80, 80);

        /// <summary>
        /// レーンのガイド線の色を設定します。
        /// </summary>
        public Color LaneBorderColor { get; set; } = Color.FromArgb(60, 60, 60);

        /// <summary>
        /// 1レーンあたりの表示幅を設定します。
        /// </summary>
        public int UnitLaneWidth { get; set; } = 12;

        /// <summary>
        /// レーンのガイド線の幅を取得します。
        /// </summary>
        public int BorderThickness { get { return (int)Math.Round(UnitLaneWidth * 0.1f); } }

        /// <summary>
        /// ショートノーツの表示高さを設定します。
        /// </summary>
        public int ShortNoteHeight { get; set; } = 5;

        /// <summary>
        /// 1拍あたりのTick数を取得します。
        /// </summary>
        public int UnitBeatTick { get { return 480; } }

        /// <summary>
        /// 1拍あたりの表示高さを設定します。
        /// </summary>
        public float UnitBeatHeight { get; set; } = 80;

        /// <summary>
        /// 表示始端のTickを設定します。
        /// </summary>
        public int HeadTick { get; set; }

        public NoteCollection Notes { get; } = new NoteCollection();

        public NoteView()
        {
            InitializeComponent();
            this.DoubleBuffered = true;
            this.BackColor = Color.Black;
            this.SetStyle(ControlStyles.ResizeRedraw, true);
            this.SetStyle(ControlStyles.Opaque, true);

            var taps = new Tap[]
            {
                new Tap() { Tick = 240, LaneIndex = 0, Width = 4 },
                new ExTap() { Tick = 240, LaneIndex = 4, Width = 3 },
                new Tap() { Tick = 240, LaneIndex = 7, Width = 4 },
                new Tap() { Tick = 480, LaneIndex = 0, Width = 4 },
                new ExTap() { Tick = 480, LaneIndex = 4, Width = 3 },
                new Tap() { Tick = 480, LaneIndex = 7, Width = 4 }
            };
            foreach (var note in taps)
            {
                Notes.Add(note);
            }
            foreach (var note in taps.Skip(3))
                Notes.Add(new Air(note) { HorizontalDirection = HorizontalAirDirection.Right, VerticalDirection = VerticalAirDirection.Up });


            taps = new Tap[]
            {
                new Tap() { Tick = 960, LaneIndex = 5, Width = 4 },
                new ExTap() { Tick = 960, LaneIndex = 9, Width = 3 },
                new Tap() { Tick = 960, LaneIndex = 12, Width = 4 }
            };
            foreach (var note in taps)
            {
                Notes.Add(note);
                Notes.Add(new Air(note) { HorizontalDirection = HorizontalAirDirection.Right, VerticalDirection = VerticalAirDirection.Down });
            }

            Notes.Add(new Flick() { Tick = 480 * 4, LaneIndex = 0, Width = 8 });
            Notes.Add(new Hold() { StartTick = 480 * 4, Duration = 480 * 4, Width = 8, LaneIndex = 8 });

            for (int i = 0; i < 4; i++)
            {
                Notes.Add(new Tap() { Tick = 240 * (1 + i) + 480 * 4, LaneIndex = i * 4, Width = 4 });
            }

            var tap = new Tap() { Tick = 480 * 6, LaneIndex = 0, Width = 3 };
            Notes.Add(new Air(tap));
            var airaction = new AirAction(tap);
            airaction.ActionNotes.Add(new AirAction.ActionNote() { Offset = 480 });

            var tap2 = new Tap() { Tick = 480 * 7, LaneIndex = 0, Width = 3 };
            var air2 = new Air(tap2) { VerticalDirection = VerticalAirDirection.Down };
            Notes.Add(tap2);
            Notes.Add(air2);
            Notes.Add(tap);
            Notes.Add(airaction);

            var slide = new Slide() { Width = 4, StartTick = 480 * 4 };
            slide.StartNote.LaneIndex = 8;
            slide.StepNotes.Add(new Slide.StepTap(slide) { Offset = 240, LaneIndex = 12 });
            slide.StepNotes.Add(new Slide.StepTap(slide) { Offset = 240 * 2, LaneIndex = 8 });
            slide.StepNotes.Add(new Slide.StepTap(slide) { Offset = 240 * 3, LaneIndex = 12 });
            slide.StepNotes.Add(new Slide.StepTap(slide) { Offset = 240 * 4, LaneIndex = 8 });
            slide.StepNotes.Add(new Slide.StepTap(slide) { Offset = 240 * 5, LaneIndex = 12 });
            Notes.Add(slide);

            HeadTick = 240;
        }

        protected override void OnPaint(PaintEventArgs pe)
        {
            base.OnPaint(pe);

            // Y軸の正方向をTick増加方向として描画 (y = 0 はコントロール下端)
            // コントロールの中心に描画したいなら後でTranslateしといてね
            var prevMatrix = pe.Graphics.Transform;
            var matrix = new Matrix();
            matrix.Scale(1, -1);
            matrix.Translate(0, ClientSize.Height - 1, MatrixOrder.Append);
            // さらにずらして下端とHeadTickを合わせる
            matrix.Translate(0, HeadTick * UnitBeatHeight / UnitBeatTick, MatrixOrder.Append);
            pe.Graphics.Transform = matrix;

            float laneWidth = UnitLaneWidth * Constants.LanesCount + BorderThickness * (Constants.LanesCount - 1);
            int tailTick = HeadTick + (int)(ClientSize.Height * UnitBeatTick / UnitBeatHeight);

            // レーン分割線描画
            using (var pen = new Pen(LaneBorderColor, BorderThickness))
            {
                for (int i = 0; i <= Constants.LanesCount; i++)
                {
                    float x = i * (UnitLaneWidth + BorderThickness);
                    pe.Graphics.DrawLine(pen, x, GetYPositionFromTick(HeadTick), x, GetYPositionFromTick(tailTick));
                }
            }


            // 時間ガイドの描画
            using (var beatPen = new Pen(BeatLineColor, BorderThickness))
            using (var barPen = new Pen(BarLineColor, BorderThickness))
            {
                for (int i = HeadTick / UnitBeatTick; i * UnitBeatTick < tailTick; i++)
                {
                    float y = i * UnitBeatHeight;
                    // 4分の4拍子で1小節ごとに小節区切り
                    pe.Graphics.DrawLine(i % 4 == 0 ? barPen : beatPen, 0, y, laneWidth, y);
                }
            }

            pe.Graphics.DrawLine(Pens.Red, 0, 0, laneWidth, 0);

            // ノート描画
            var holds = Notes.Holds.Where(p => p.StartTick >= HeadTick && p.StartTick <= tailTick).ToList();
            // ロングノーツ背景
            // HOLD
            foreach (var hold in holds)
            {
                hold.DrawBackground(pe.Graphics, new RectangleF(
                    (UnitLaneWidth + BorderThickness) * hold.LaneIndex + BorderThickness,
                    GetYPositionFromTick(hold.StartTick),
                    (UnitLaneWidth + BorderThickness) * hold.Width - BorderThickness,
                    GetYPositionFromTick(hold.Duration)
                    ));
            }

            // SLIDE
            var slides = Notes.Slides.Where(p => p.StartTick >= HeadTick && p.StartTick <= tailTick).ToList();
            foreach (var slide in slides)
            {
                var bg = new Slide.TapBase[] { slide.StartNote }.Concat(slide.StepNotes.OrderBy(p => p.Tick)).ToList();
                for (int i = 0; i < bg.Count - 1; i++)
                {
                    slide.DrawBackground(pe.Graphics,
                        (UnitLaneWidth + BorderThickness) * slide.Width - BorderThickness,
                        (UnitLaneWidth + BorderThickness) * bg[i].LaneIndex,
                        GetYPositionFromTick(bg[i].Tick),
                        (UnitLaneWidth + BorderThickness) * bg[i + 1].LaneIndex,
                        GetYPositionFromTick(bg[i + 1].Tick),
                        ShortNoteHeight);
                }
            }

            // AIR-ACTION(ガイド線)
            var airActions = Notes.AirActions.Where(p => p.StartTick >= HeadTick && p.StartTick <= tailTick).ToList();
            foreach (var note in airActions)
            {
                note.DrawLine(pe.Graphics,
                    (UnitLaneWidth + BorderThickness) * note.ParentNote.LaneIndex + BorderThickness + UnitLaneWidth * note.ParentNote.Width / 2,
                    GetYPositionFromTick(note.StartTick),
                    GetYPositionFromTick(note.StartTick + note.GetDuration()),
                    ShortNoteHeight);
            }

            // ショートノーツ
            // HOLD始点
            foreach (var hold in holds)
            {
                hold.StartNote.Draw(pe.Graphics, GetRectFromNotePosition(hold.StartTick, hold.LaneIndex, hold.Width));
                hold.EndNote.Draw(pe.Graphics, GetRectFromNotePosition(hold.StartTick + hold.Duration, hold.LaneIndex, hold.Width));
            }

            // SLIDE始点
            foreach (var slide in slides)
            {
                slide.StartNote.Draw(pe.Graphics, GetRectFromNotePosition(slide.StartTick, slide.StartNote.LaneIndex, slide.Width));
                foreach (var step in slide.StepNotes)
                {
                    if (!step.IsVisible) continue;
                    step.Draw(pe.Graphics, GetRectFromNotePosition(step.Tick, step.LaneIndex, step.Width));
                }
            }

            // TAP, ExTAP, FLICK, DAMAGE
            foreach (var note in Notes.Taps.Where(p => p.Tick >= HeadTick && p.Tick <= tailTick))
            {
                note.Draw(pe.Graphics, GetRectFromNotePosition(note.Tick, note.LaneIndex, note.Width));
            }

            foreach (var note in Notes.Flicks.Where(p => p.Tick >= HeadTick && p.Tick <= tailTick))
            {
                note.Draw(pe.Graphics, GetRectFromNotePosition(note.Tick, note.LaneIndex, note.Width));
            }

            // AIR-ACTION(ActionNote)
            foreach (var action in airActions)
            {
                foreach (var note in action.ActionNotes)
                {
                    note.Draw(pe.Graphics, GetRectFromNotePosition(action.StartTick + note.Offset, action.ParentNote.LaneIndex, action.ParentNote.Width).Expand(-ShortNoteHeight * 0.28f));
                }
            }

            // AIR
            foreach (var note in Notes.Airs.Where(p => p.Tick >= HeadTick && p.Tick <= tailTick))
            {
                note.Draw(pe.Graphics, GetRectFromNotePosition(note.ParentNote.Tick, note.ParentNote.LaneIndex, note.ParentNote.Width));
            }


            pe.Graphics.Transform = prevMatrix;
        }

        private float GetYPositionFromTick(int tick)
        {
            return tick * UnitBeatHeight / UnitBeatTick;
        }

        private RectangleF GetRectFromNotePosition(int tick, int laneIndex, int width)
        {
            return new RectangleF(
                (UnitLaneWidth + BorderThickness) * laneIndex + BorderThickness,
                GetYPositionFromTick(tick) - ShortNoteHeight / 2,
                (UnitLaneWidth + BorderThickness) * width - BorderThickness,
                ShortNoteHeight
                );
        }

        public class NoteCollection
        {
            public event EventHandler NoteChanged;

            private List<Tap> taps;
            public IReadOnlyCollection<Tap> Taps { get { return taps; } }

            private List<Hold> holds;
            public IReadOnlyCollection<Hold> Holds { get { return holds; } }

            private List<Slide> slides;
            public IReadOnlyCollection<Slide> Slides { get { return slides; } }

            private List<Air> airs;
            public IReadOnlyCollection<Air> Airs { get { return airs; } }

            private List<AirAction> airActions;
            public IReadOnlyCollection<AirAction> AirActions { get { return airActions; } }

            private List<Flick> flicks;
            public IReadOnlyCollection<Flick> Flicks { get { return flicks; } }

            private List<Damage> damages;
            public IReadOnlyCollection<Damage> Damages { get { return damages; } }

            public NoteCollection()
            {
                taps = new List<Tap>();
                holds = new List<Hold>();
                slides = new List<Slide>();
                airs = new List<Air>();
                airActions = new List<AirAction>();
                flicks = new List<Flick>();
                damages = new List<Damage>();
            }

            public void Add(Tap note)
            {
                taps.Add(note);
                NoteChanged?.Invoke(this, EventArgs.Empty);
            }

            public void Add(Hold note)
            {
                holds.Add(note);
                NoteChanged?.Invoke(this, EventArgs.Empty);
            }

            public void Add(Slide note)
            {
                slides.Add(note);
                NoteChanged?.Invoke(this, EventArgs.Empty);
            }

            public void Add(Air note)
            {
                airs.Add(note);
                NoteChanged?.Invoke(this, EventArgs.Empty);
            }

            public void Add(AirAction note)
            {
                airActions.Add(note);
                NoteChanged?.Invoke(this, EventArgs.Empty);
            }

            public void Add(Flick note)
            {
                flicks.Add(note);
                NoteChanged?.Invoke(this, EventArgs.Empty);
            }

            public void Add(Damage note)
            {
                damages.Add(note);
                NoteChanged?.Invoke(this, EventArgs.Empty);
            }


            public void Remove(Tap note)
            {
                taps.Remove(note);
                NoteChanged?.Invoke(this, EventArgs.Empty);
            }

            public void Remove(Hold note)
            {
                holds.Remove(note);
                NoteChanged?.Invoke(this, EventArgs.Empty);
            }

            public void Remove(Slide note)
            {
                slides.Remove(note);
                NoteChanged?.Invoke(this, EventArgs.Empty);
            }

            public void Remove(Air note)
            {
                airs.Remove(note);
                NoteChanged?.Invoke(this, EventArgs.Empty);
            }

            public void Remove(AirAction note)
            {
                airActions.Remove(note);
                NoteChanged?.Invoke(this, EventArgs.Empty);
            }

            public void Remove(Flick note)
            {
                flicks.Remove(note);
                NoteChanged?.Invoke(this, EventArgs.Empty);
            }

            public void Remove(Damage note)
            {
                damages.Remove(note);
                NoteChanged?.Invoke(this, EventArgs.Empty);
            }


            public void Clear()
            {
                taps.Clear();
                holds.Clear();
                slides.Clear();
                airs.Clear();
                airActions.Clear();
                flicks.Clear();
                damages.Clear();
                NoteChanged?.Invoke(this, EventArgs.Empty);
            }
        }
    }
}
