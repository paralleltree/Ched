using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Forms;
using System.Windows.Forms.Integration;

namespace Ched.UI.Windows
{
    /// <summary>
    /// Interaction logic for BindableNumericUpDown.xaml
    /// </summary>
    public partial class BindableNumericUpDown : WindowsFormsHost, INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        public static readonly DependencyProperty ValueProperty = DependencyProperty.Register("Value", typeof(decimal), typeof(BindableNumericUpDown),
            new FrameworkPropertyMetadata(0m, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, new PropertyChangedCallback(OnValueChanged)));
        public decimal Value
        {
            get => (decimal)GetValue(ValueProperty);
            set
            {
                SetValue(ValueProperty, value);
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Value)));
            }
        }

        public static readonly DependencyProperty MinimumProperty = DependencyProperty.Register("Minimum", typeof(decimal), typeof(BindableNumericUpDown),
            new FrameworkPropertyMetadata(0m, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, new PropertyChangedCallback(OnMinimumChanged)));
        public decimal Minimum
        {
            get => (decimal)GetValue(MinimumProperty);
            set
            {
                SetValue(MinimumProperty, value);
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Minimum)));
            }
        }

        public static readonly DependencyProperty MaximumProperty = DependencyProperty.Register("Maximum", typeof(decimal), typeof(BindableNumericUpDown),
            new FrameworkPropertyMetadata(100m, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, new PropertyChangedCallback(OnMaximumChanged)));
        public decimal Maximum
        {
            get => (decimal)GetValue(MaximumProperty);
            set
            {
                SetValue(MaximumProperty, value);
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Maximum)));
            }
        }

        public static readonly DependencyProperty IncrementProperty = DependencyProperty.Register("Increment", typeof(decimal), typeof(BindableNumericUpDown),
           new FrameworkPropertyMetadata(1m, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, new PropertyChangedCallback(OnIncrementChanged)));
        public decimal Increment
        {
            get => (decimal)GetValue(IncrementProperty);
            set
            {
                SetValue(IncrementProperty, value);
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Increment)));
            }
        }

        public static readonly DependencyProperty DecimalPlacesProperty = DependencyProperty.Register("DecimalPlaces", typeof(int), typeof(BindableNumericUpDown),
            new FrameworkPropertyMetadata(0, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, new PropertyChangedCallback(OnDecimalPlacesChanged)));
        public int DecimalPlaces
        {
            get => (int)GetValue(DecimalPlacesProperty);
            set
            {
                SetValue(DecimalPlacesProperty, value);
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(DecimalPlaces)));
            }
        }

        public BindableNumericUpDown()
        {
            InitializeComponent();
            foreach (string prop in new[] { "Value", "Minimum", "Maximum", "Increment", "DecimalPlaces" })
                InitializePropertyBinding(prop);
        }

        private void InitializePropertyBinding(string name)
        {
            var src = new BindingSource();
            var initializer = (ISupportInitialize)src;
            initializer.BeginInit();
            var child = (NumericUpDown)Child;
            child.DataBindings.Add(new System.Windows.Forms.Binding(name, src, name, true, DataSourceUpdateMode.OnPropertyChanged));
            src.DataSource = typeof(BindableNumericUpDown);
            initializer.EndInit();
            src.DataSource = this;
        }

        private static void OnValueChanged(DependencyObject sender, DependencyPropertyChangedEventArgs e)
        {
            var control = sender as BindableNumericUpDown;
            if (control == null) return;
            if (e.Property == ValueProperty) control.Value = (decimal)e.NewValue;
        }

        private static void OnMinimumChanged(DependencyObject sender, DependencyPropertyChangedEventArgs e)
        {
            var control = sender as BindableNumericUpDown;
            if (control == null) return;
            if (e.Property == MinimumProperty) control.Minimum = (decimal)e.NewValue;
        }

        private static void OnMaximumChanged(DependencyObject sender, DependencyPropertyChangedEventArgs e)
        {
            var control = sender as BindableNumericUpDown;
            if (control == null) return;
            if (e.Property == MaximumProperty) control.Maximum = (decimal)e.NewValue;
        }

        private static void OnIncrementChanged(DependencyObject sender, DependencyPropertyChangedEventArgs e)
        {
            var control = sender as BindableNumericUpDown;
            if (control == null) return;
            if (e.Property == IncrementProperty) control.Increment = (decimal)e.NewValue;
        }

        private static void OnDecimalPlacesChanged(DependencyObject sender, DependencyPropertyChangedEventArgs e)
        {
            var control = sender as BindableNumericUpDown;
            if (control == null) return;
            if (e.Property == DecimalPlacesProperty) control.DecimalPlaces = (int)e.NewValue;
        }
    }
}
