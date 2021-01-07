using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using Microsoft.Xaml.Behaviors;

namespace Ched.UI.Windows.Behaviors
{
    public class UpdateTextOnEnterPressBehavior : Behavior<TextBox>
    {
        protected override void OnAttached()
        {
            base.OnAttached();
            AssociatedObject.PreviewKeyDown += OnPreviewKeyDown;
        }

        protected override void OnDetaching()
        {
            base.OnDetaching();
            AssociatedObject.PreviewKeyDown -= OnPreviewKeyDown;
        }

        private void OnPreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key != Key.Enter) return;
            var control = (Control)sender;
            var binding = BindingOperations.GetBindingExpression(control, TextBox.TextProperty);
            binding?.UpdateSource();
        }
    }

    public class UpdateNumericUpDownValueOnEnterPressBehavior : Behavior<BindableNumericUpDown>
    {
        protected override void OnAttached()
        {
            base.OnAttached();
            AssociatedObject.PreviewKeyDown += OnPreviewKeyDown;
        }

        protected override void OnDetaching()
        {
            base.OnDetaching();
            AssociatedObject.PreviewKeyDown -= OnPreviewKeyDown;
        }

        private void OnPreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key != Key.Enter) return;
            var control = (BindableNumericUpDown)sender;
            // フォーカスを移動させて強制validateする最悪実装
            control.MoveFocus(new TraversalRequest(FocusNavigationDirection.Next));
        }
    }
}
