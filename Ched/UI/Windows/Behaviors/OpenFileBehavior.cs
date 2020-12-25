using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Microsoft.Xaml.Behaviors;

namespace Ched.UI.Windows.Behaviors
{
    public class OpenFileBehavior : Behavior<Button>
    {
        public static readonly DependencyProperty FilterProperty = DependencyProperty.RegisterAttached("Filter", typeof(string), typeof(OpenFileBehavior), new FrameworkPropertyMetadata(null));
        public string Filter
        {
            get => (string)GetValue(FilterProperty);
            set => SetValue(FilterProperty, value);
        }

        public static readonly DependencyProperty CallbackActionProperty = DependencyProperty.RegisterAttached("CallbackAction", typeof(Action<string>), typeof(OpenFileBehavior), new FrameworkPropertyMetadata(null));
        public Action<string> CallbackAction
        {
            get => (Action<string>)GetValue(CallbackActionProperty);
            set => SetValue(CallbackActionProperty, value);
        }

        protected override void OnAttached()
        {
            base.OnAttached();
            AssociatedObject.Click += OnClick;
        }

        protected override void OnDetaching()
        {
            base.OnDetaching();
            AssociatedObject.Click -= OnClick;
        }

        private void OnClick(object sender, RoutedEventArgs e)
        {
            var dialog = new Microsoft.Win32.OpenFileDialog()
            {
                Filter = Filter
            };
            var result = dialog.ShowDialog();
            if (result.HasValue && result.Value)
            {
                CallbackAction?.Invoke(dialog.FileName);
            }
        }
    }
}
