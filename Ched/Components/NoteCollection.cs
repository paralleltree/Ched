using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Ched.Components.Notes;

namespace Ched.Components
{
    /// <summary>
    /// ノーツを格納するコレクションを表すクラスです。
    /// </summary>
    public class NoteCollection
    {
        public List<Tap> Taps { get; set; }
        public List<Hold> Holds { get; set; }
        public List<Slide> Slides { get; set; }
        public List<Air> Airs { get; set; }
        public List<AirAction> AirActions { get; set; }
        public List<Flick> Flicks { get; set; }
        public List<Damage> Damages { get; set; }

        public NoteCollection()
        {
            Taps = new List<Tap>();
            Holds = new List<Hold>();
            Slides = new List<Slide>();
            Airs = new List<Air>();
            AirActions = new List<AirAction>();
            Flicks = new List<Flick>();
            Damages = new List<Damage>();
        }

        public NoteCollection(UI.NoteView.NoteCollection collection)
        {
            Taps = collection.Taps.ToList();
            Holds = collection.Holds.ToList();
            Slides = collection.Slides.ToList();
            Airs = collection.Airs.ToList();
            AirActions = collection.AirActions.ToList();
            Flicks = collection.Flicks.ToList();
            Damages = collection.Damages.ToList();
        }
    }
}
