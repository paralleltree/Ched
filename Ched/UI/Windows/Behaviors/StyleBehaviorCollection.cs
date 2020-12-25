using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using Microsoft.Xaml.Behaviors;

namespace Ched.UI.Windows.Behaviors
{
    public class StyleBehaviorCollection : FreezableCollection<Behavior>
    {
        public static readonly DependencyProperty StyleBehaviorsProperty = DependencyProperty.RegisterAttached("StyleBehaviors", typeof(StyleBehaviorCollection), typeof(StyleBehaviorCollection), new PropertyMetadata(OnStyleBehaviorsChanged));

        public static void OnStyleBehaviorsChanged(DependencyObject sender, DependencyPropertyChangedEventArgs e)
        {
            if (e.OldValue == e.NewValue) return;

            var value = e.NewValue as StyleBehaviorCollection;
            if (value == null) return;

            var behaviors = Interaction.GetBehaviors(sender);
            behaviors.Clear();
            foreach (var b in value.Select(x => (Behavior)x.Clone())) behaviors.Add(b);
        }

        public static StyleBehaviorCollection GetStyleBehaviors(DependencyObject obj) => (StyleBehaviorCollection)obj.GetValue(StyleBehaviorsProperty);

        public static void SetStyleBehaviors(DependencyObject obj, StyleBehaviorCollection value) => obj.SetValue(StyleBehaviorsProperty, value);

        protected override Freezable CreateInstanceCore() => new StyleBehaviorCollection();
    }
}
