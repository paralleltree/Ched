using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Text;
using System.Threading.Tasks;

using System.ComponentModel.Composition;
using System.ComponentModel.Composition.Hosting;
using System.ComponentModel.Composition.Registration;
using System.Reflection;

namespace Ched.Plugins
{
    public class PluginManager
    {
        internal static string PluginPath => "Plugins";

        [ImportMany]
        IEnumerable<IScorePlugin> scorePlugins = Enumerable.Empty<IScorePlugin>();
        [ImportMany]
        IEnumerable<IScoreBookImportPlugin> bookImportPlugins = Enumerable.Empty<IScoreBookImportPlugin>();

        public List<string> FailedFiles { get; private set; } = new List<string>();
        public List<string> InvalidFiles { get; private set; } = new List<string>();

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

            var failedFiles = new List<string>();
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
                        failedFiles.Add(GetRelativePluginPath(path));
                    }
                }
            }

            var container = new CompositionContainer(catalog);
            PluginManager manager = null;
            try
            {
                manager = container.GetExportedValue<PluginManager>();
            }
            catch (ReflectionTypeLoadException ex) when (ex.LoaderExceptions.Any(p => p is TypeLoadException))
            {
                return new PluginManager()
                {
                    FailedFiles = failedFiles,
                    InvalidFiles = ex.Types.Where(p => p != null).Select(p => GetRelativePluginPath(p.Assembly.Location)).Distinct().ToList()
                };
            }
            manager.FailedFiles = failedFiles;
            return manager;
        }

        private static string GetRelativePluginPath(string path) => Uri.UnescapeDataString(new Uri(Path.GetFullPath(PluginPath)).MakeRelativeUri(new Uri(path)).ToString().Replace('/', Path.DirectorySeparatorChar));
    }
}
