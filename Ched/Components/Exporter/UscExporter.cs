using Ched.Core;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Markup;

namespace Ched.Components.Exporter
{
    public class UscExporter
    {

        protected ScoreBook ScoreBook { get; set; }
        protected BarIndexCalculator BarIndexCalculator { get; }
        protected int StandardBarTick => ScoreBook.Score.TicksPerBeat * 4;

        public UscExporter(ScoreBook book)
        {
            ScoreBook = book;
            BarIndexCalculator = new BarIndexCalculator(book.Score.TicksPerBeat, book.Score.Events.TimeSignatureChangeEvents);
        }

        public void Export(Stream stream)
        {
            var book = ScoreBook;
            var notes = book.Score.Notes;

            



        }

    }
}
