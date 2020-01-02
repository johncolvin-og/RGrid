using RGrid.Utility;
using RGrid.WPF;
using System.Windows;
using System.Windows.Controls;

namespace RGrid.Controls {
   public class TickUpDownNullableDouble : Control {
      static TickUpDownNullableDouble() =>
         DefaultStyleKeyProperty.OverrideMetadata(typeof(TickUpDownNullableDouble), new FrameworkPropertyMetadata(typeof(TickUpDownNullableDouble)));

      public static readonly DependencyProperty ValueProperty = DependencyProperty.Register("Value", typeof(double?), typeof(TickUpDownNullableDouble),
         new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault));
      public double? Value { get => GetValue(ValueProperty) as double?; set => SetValue(ValueProperty, value); }

      public static readonly DependencyProperty IncrementProperty = DependencyProperty.Register("Increment", typeof(double), typeof(TickUpDownNullableDouble),
         new PropertyMetadata(1d));
      public double Increment { get { return (double)GetValue(IncrementProperty); } set { SetValue(IncrementProperty, value); } }

      public static readonly DependencyProperty TickDefaultValueProperty = DependencyProperty.Register(nameof(TickDefaultValue), typeof(double), typeof(TickUpDownNullableDouble),
         new PropertyMetadata(0d));
      public double TickDefaultValue { get => (double)GetValue(TickDefaultValueProperty); set => SetValue(TickDefaultValueProperty, value); }

      public override void OnApplyTemplate() {
         var tick_up_down = this.assert_template_child<TickUpDown>("PART_tick_up_down");
         tick_up_down.TickUpCommand = new DelegateCommand(_tick_up);
         tick_up_down.TickDownCommand = new DelegateCommand(_tick_down);
      }

      void _tick_up() {
         double? v = Value;
         Value = v.HasValue && !DoubleHelper.IsNaNOrInfinity(v.Value) ? v.Value + Increment : TickDefaultValue;
      }

      void _tick_down() {
         double? v = Value;
         if (v.HasValue && !DoubleHelper.IsNaNOrInfinity(v.Value)) {
            Value = v.Value - Increment;
         } else Value = TickDefaultValue;
      }
   }
}