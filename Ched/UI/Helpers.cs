using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ched.UI
{
    public static class Helpers
    {
        public static string GetFilterString(string kind, IEnumerable<string> extensions)
        {
            var wildcards = extensions.Select(p => "*" + p);
            return kind + string.Format("({0})|{1}", string.Join(", ", wildcards), string.Join(";", wildcards));
        }
    }
}
