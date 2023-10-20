using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ched.Plugins
{
    public class ScoreBookImportPluginArgs : IScoreBookImportPluginArgs
    {
        private readonly List<Diagnostic> _diagnostics = new List<Diagnostic>();
        public IReadOnlyCollection<Diagnostic> Diagnostics => _diagnostics;
        public Stream Stream { get; }
        public string Path { get; }

        public ScoreBookImportPluginArgs(Stream stream, string path)
        {
            Stream = stream;
            Path = path;
        }

        public void ReportDiagnostic(Diagnostic diagnostic)
        {
            _diagnostics.Add(diagnostic);
        }
    }
}
