using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Markup;

namespace Ched.UI.Windows
{
    public class EnumSourceProvider<T> : MarkupExtension
    {
        private static string DisplayName(T value)
        {
            var field = value.GetType().GetField(value.ToString());
            var attr = (DescriptionAttribute)field.GetCustomAttributes(typeof(DescriptionAttribute), false).FirstOrDefault();
            return attr.Description;
        }

        public System.Collections.IEnumerable Source { get; } = typeof(T).GetEnumValues().Cast<T>().Select(p => new { Code = p, Name = DisplayName(p) });

        public override object ProvideValue(IServiceProvider serviceProvider) => this;
    }
}
