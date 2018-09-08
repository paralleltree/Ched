using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.ComponentModel.Composition;
using System.ComponentModel.Composition.Hosting;
using System.ComponentModel.Composition.Registration;

namespace Ched.Plugins
{
    public class PluginManager
    {
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
            // import dlls

            var container = new CompositionContainer(catalog);
            return container.GetExportedValue<PluginManager>();
        }
    }
}
