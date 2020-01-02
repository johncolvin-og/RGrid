using RGrid.Utility;
using RGrid.WPF;
using System.Windows;
using System.Windows.Controls;

namespace RGrid.Controls {
   public class TickUpDownDouble : Control {
      static TickUpDownDouble() =>
         DefaultStyleKeyProperty.OverrideMetadata(typeof(TickUpDownDouble), new FrameworkPropertyMetadata(typeof(TickUpDownDouble)));

      public static readonly DependencyProperty ValueProperty = DependencyProperty.Register("Value", typeof(double), typeof(TickUpDownDouble), new FrameworkPropertyMetadata(0.0, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault));
      public static readonly DependencyProperty IncrementProperty = DependencyProperty.Register("Increment", typeof(double), typeof(TickUpDownDouble), new FrameworkPropertyMetadata(1.0));
      public static readonly DependencyProperty MultiplierProperty = DependencyProperty.Register("Multiplier", typeof(double), typeof(TickUpDownDouble), new FrameworkPropertyMetadata(1.0));
      public static readonly DependencyProperty FormatProperty = DependencyProperty.Register("Format", typeof(string), typeof(TickUpDownDouble));
      public static readonly DependencyProperty TickDefaultValueProperty = DependencyProperty.Register(nameof(TickDefaultValue), typeof(double), typeof(TickUpDownDouble), new PropertyMetadata(0d));

      public double Value { get { return (double)GetValue(ValueProperty); } set { SetValue(ValueProperty, value); } }
      public double Increment { get { return (double)GetValue(IncrementProperty); } set { SetValue(IncrementProperty, value); } }
      public double Multiplier { get { return (double)GetValue(MultiplierProperty); } set { SetValue(MultiplierProperty, value); } }
      public string Format { get { return GetValue(FormatProperty) as string; } set { SetValue(FormatProperty, value); } }
      public double TickDefaultValue { get => (double)GetValue(TickDefaultValueProperty); set => SetValue(TickDefaultValueProperty, value); }

      public override void OnApplyTemplate() {
         var tick_up_down = this.assert_template_child<TickUpDown>("PART_tick_up_down");
         tick_up_down.TickUpCommand = new DelegateCommand(_tick_up);
         tick_up_down.TickDownCommand = new DelegateCommand(_tick_down);
      }

      void _tick_up() {
         double v = Value;
         Value = !DoubleHelper.IsNaNOrInfinity(v) ? v + Increment : TickDefaultValue;
      }

      void _tick_down() {
         double v = Value;
         if (!DoubleHelper.IsNaNOrInfinity(v)) {
            Value = v - Increment;
         } else Value = TickDefaultValue;
      }
   }
}