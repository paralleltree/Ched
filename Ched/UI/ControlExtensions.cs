﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Reactive.Linq;

namespace Ched.UI
{
    internal static class ControlExtensions
    {
        public static LayoutManager WorkWithLayout(this Control control)
        {
            return new LayoutManager(control);
        }

        public static void InvokeIfRequired(this Control control, Action action)
        {
            if (control.InvokeRequired) control.Invoke((MethodInvoker)(() => action()));
            else action();
        }

        public static IObservable<MouseEventArgs> MouseDownAsObservable(this Control control)
        {
            return Observable.FromEvent<MouseEventHandler, MouseEventArgs>(
                     h => (o, e) => h(e),
                     h => control.MouseDown += h,
                     h => control.MouseDown -= h);
        }

        public static IObservable<MouseEventArgs> MouseMoveAsObservable(this Control control)
        {
            return Observable.FromEvent<MouseEventHandler, MouseEventArgs>(
                     h => (o, e) => h(e),
                     h => control.MouseMove += h,
                     h => control.MouseMove -= h);
        }

        public static IObservable<MouseEventArgs> MouseUpAsObservable(this Control control)
        {
            return Observable.FromEvent<MouseEventHandler, MouseEventArgs>(
                     h => (o, e) => h(e),
                     h => control.MouseUp += h,
                     h => control.MouseUp -= h);
        }

        public static int GetMaximumValue(this ScrollBar scrollbar)
        {
            return scrollbar.Maximum - scrollbar.LargeChange + 1;
        }

        public static void SelectAll(this NumericUpDown control)
        {
            control.Select(0, control.Text.Length);
        }
    }

    internal class LayoutManager : IDisposable
    {
        protected Control _control;

        public LayoutManager(Control control)
        {
            control.SuspendLayout();
            _control = control;
        }

        public void Dispose()
        {
            _control.ResumeLayout(false);
            _control.PerformLayout();
        }
    }
}
