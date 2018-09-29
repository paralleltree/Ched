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

        public IEnumerable<IScorePlugin> ScorePlugins => scorePlugins;

        private PluginManager()
        {
        }

        public static PluginManager GetInstance()
        {
            var builder = new RegistrationBuilder();
            builder.ForTypesDerivedFrom<IPlugin>().ExportInterfaces();
            builder.ForType<PluginManager>().Export<PluginManager>();

            var self = new AssemblyCatalog(typeof(PluginManager).Assembly, builder);
            var catalog = new AggregateCatalog(self);

            if (Directory.Exists(PluginPath))
            {
                foreach (string path in new DirectoryInfo(PluginPath).GetFiles().Select(p => p.FullName).Where(p => p.ToLower().EndsWith(".dll")))
                {
                    var assembly = System.Reflection.Assembly.LoadFile(path);
                    catalog.Catalogs.Add(new AssemblyCatalog(assembly, builder));
                }
            }

            var container = new CompositionContainer(catalog);
            return container.GetExportedValue<PluginManager>();
        }
    }
}
