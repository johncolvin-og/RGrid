using RGrid.WPF;
using System.Windows;
using System.Windows.Controls;

namespace RGrid.Controls {
   public class TickUpDownNullableULong : Control {
      static TickUpDownNullableULong() =>
         DefaultStyleKeyProperty.OverrideMetadata(typeof(TickUpDownNullableULong), new FrameworkPropertyMetadata(typeof(TickUpDownNullableULong)));

      public static readonly DependencyProperty ValueProperty = DependencyProperty.Register("Value", typeof(ulong?), typeof(TickUpDownNullableULong),
         new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault));
      public ulong? Value { get => GetValue(ValueProperty) as ulong?; set => SetValue(ValueProperty, value); }

      public static readonly DependencyProperty IncrementProperty = DependencyProperty.Register("Increment", typeof(ulong), typeof(TickUpDownNullableULong),
         new PropertyMetadata(1ul));
      public ulong Increment { get { return (ulong)GetValue(IncrementProperty); } set { SetValue(IncrementProperty, value); } }

      public static readonly DependencyProperty TickDefaultValueProperty = DependencyProperty.Register(nameof(TickDefaultValue), typeof(ulong), typeof(TickUpDownNullableULong),
         new PropertyMetadata(0ul));
      public ulong TickDefaultValue { get => (ulong)GetValue(TickDefaultValueProperty); set => SetValue(TickDefaultValueProperty, value); }

      public override void OnApplyTemplate() {
         var tick_up_down = this.assert_template_child<TickUpDown>("PART_tick_up_down");
         tick_up_down.TickUpCommand = new DelegateCommand(_tick_up);
         tick_up_down.TickDownCommand = new DelegateCommand(_tick_down);
      }

      void _tick_up() {
         ulong? v = Value;
         Value = v.HasValue ? v.Value + Increment : TickDefaultValue;
      }

      void _tick_down() {
         ulong? v = Value;
         if (v.HasValue) {
            // do not tick across 0
            Value = v.Value > Increment ? v.Value - Increment : 0ul;
         } else Value = TickDefaultValue;
      }
   }
}