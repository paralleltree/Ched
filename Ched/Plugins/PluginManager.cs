using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Text;
using System.Threading.Tasks;

using System.ComponentModel.Composition;
using System.ComponentModel.Composition.Hosting;
using System.ComponentModel.Composition.Registration;

namespace Ched.Plugins
{
    public class PluginManager
    {
        protected static string PluginPath => "Plugins";

        [ImportMany]
        IEnumerable<IScorePlugin> scorePlugins = Enumerable.Empty<IScorePlugin>();
        [ImportMany]
        IEnumerable<IScoreBookImportPlugin> bookImportPlugins = Enumerable.Empty<IScoreBookImportPlugin>();

        public List<string> FailedFiles { get; private set; } = new List<string>();

        public IEnumerable<IScorePlugin> ScorePlugins => scorePlugins;
        public IEnumerable<IScoreBookImportPlugin> ScoreBookImportPlugins => bookImportPlugins;

        private PluginManager()
        {
        }

        public static PluginManager GetInstance()
        {
            var builder = new RegistrationBuilder();
            builder.ForTypesDerivedFrom<IPlugin>().ExportInterfaces();
            builder.ForType<PluginManager>().Export<PluginManager>();

            var failed = new List<string>();
            var self = new AssemblyCatalog(typeof(PluginManager).Assembly, builder);
            var catalog = new AggregateCatalog(self);

            if (Directory.Exists(PluginPath))
            {
                foreach (string path in new DirectoryInfo(PluginPath).GetFiles().Select(p => p.FullName).Where(p => p.ToLower().EndsWith(".dll")))
                {
                    try
                    {
                        var assembly = System.Reflection.Assembly.LoadFile(path);
                        catalog.Catalogs.Add(new AssemblyCatalog(assembly, builder));
                    }
                    catch (Exception ex) when (ex is NotSupportedException || ex is BadImageFormatException)
                    {
                        failed.Add(Uri.UnescapeDataString(new Uri(Path.GetFullPath(PluginPath)).MakeRelativeUri(new Uri(path)).ToString().Replace('/', Path.DirectorySeparatorChar)));
                    }
                }
            }

            var container = new CompositionContainer(catalog);
            var manager = container.GetExportedValue<PluginManager>();
            manager.FailedFiles = failed;
            return manager;
        }
    }
}
