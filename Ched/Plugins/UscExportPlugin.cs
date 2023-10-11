using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

using Newtonsoft.Json;
using Ched.Components.Exporter;
using Ched.UI.Windows;


namespace Ched.Plugins
{
    public class UscExportPlugin : IScoreBookExportPlugin
    {
        public string DisplayName => "Universal Sekai Chart (*.usc)";

        public string FileFilter => "Universal Sekai Chart (*.usc)|*.usc";

        public void Export(IScoreBookExportPluginArgs args)
        {

            var book = args.GetScoreBook();
            
            var exporter = new UscExporter(book);
            exporter.Export(args.Stream);
        }
    }
}
