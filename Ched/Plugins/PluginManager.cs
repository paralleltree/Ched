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
                catalog.Catalogs.Add(new DirectoryCatalog(PluginPath));

            var container = new CompositionContainer(catalog);
            return container.GetExportedValue<PluginManager>();
        }
    }
}
