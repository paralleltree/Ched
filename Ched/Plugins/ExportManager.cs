using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Ched.Core;

namespace Ched.Plugins
{
    internal class ExportManager
    {
        private IScoreBookExportPlugin LastUsedPlugin { get; set; }
        private string LastOutputPath { get; set; }
        private Dictionary<string, string> CustomDataCache { get; set; }

        public bool CanReExport => LastUsedPlugin != null;

        protected void Initialize()
        {
            LastUsedPlugin = null;
            CustomDataCache = null;
        }

        public void Load(ScoreBook book)
        {
            Initialize();
            CustomDataCache = book.ExportArgs;
        }

        public ExportContext PrepareExport(IScoreBookExportPlugin plugin, string dest)
        {
            return PrepareExport(plugin, dest, false);
        }

        public ExportContext PrepareReExport()
        {
            if (!CanReExport) throw new InvalidOperationException();
            return PrepareExport(LastUsedPlugin, LastOutputPath, true);
        }

        protected ExportContext PrepareExport(IScoreBookExportPlugin plugin, string dest, bool isQuick)
        {
            string name = ResolvePluginName(plugin);
            LastOutputPath = dest;
            LastUsedPlugin = plugin;
            return new ExportContext(plugin, dest, isQuick, () => CustomDataCache.ContainsKey(name) ? CustomDataCache[name] : "", data => CustomDataCache[name] = data);
        }

        protected string ResolvePluginName(IScoreBookExportPlugin plugin) => plugin.GetType().FullName;
    }

    internal class ExportContext
    {
        protected IScoreBookExportPlugin ExportPlugin { get; }
        protected bool IsQuick { get; }
        protected string OutputPath { get; }
        protected readonly Func<string> GetCustomData;
        protected readonly Action<string> SetCustomData;
        public IReadOnlyCollection<Diagnostic> Diagnostics { get; private set; }

        public ExportContext(IScoreBookExportPlugin plugin, string dest, bool isQuick, Func<string> getCustomData, Action<string> setCustomData)
        {
            ExportPlugin = plugin;
            IsQuick = isQuick;
            OutputPath = dest;
            GetCustomData = getCustomData;
            SetCustomData = setCustomData;
        }

        public void Export(ScoreBook book)
        {
            using (var ms = new MemoryStream())
            {
                var args = new ScoreBookExportPluginArgs(book, ms, IsQuick, GetCustomData, SetCustomData);
                Diagnostics = args.Diagnostics;
                ExportPlugin.Export(args);
                using (var fs = new FileStream(OutputPath, FileMode.Create, FileAccess.Write))
                {
                    var res = ms.ToArray();
                    fs.Write(res, 0, res.Length);
                }
            }
        }
    }
}
