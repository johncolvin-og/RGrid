using RGrid.WPF;
using System.Windows;
using System.Windows.Controls;

namespace RGrid.Controls {
   public class TickUpDownNullableUShort : Control {
      static TickUpDownNullableUShort() =>
         DefaultStyleKeyProperty.OverrideMetadata(typeof(TickUpDownNullableUShort), new FrameworkPropertyMetadata(typeof(TickUpDownNullableUShort)));

      public static readonly DependencyProperty ValueProperty = DependencyProperty.Register("Value", typeof(ushort?), typeof(TickUpDownNullableUShort),
         new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault));
      public ushort? Value { get => GetValue(ValueProperty) as ushort?; set => SetValue(ValueProperty, value); }

      public static readonly DependencyProperty IncrementProperty = DependencyProperty.Register("Increment", typeof(ushort), typeof(TickUpDownNullableUShort),
         new PropertyMetadata((ushort)1));
      public ushort Increment { get { return (ushort)GetValue(IncrementProperty); } set { SetValue(IncrementProperty, value); } }

      public static readonly DependencyProperty TickDefaultValueProperty = DependencyProperty.Register(nameof(TickDefaultValue), typeof(ushort), typeof(TickUpDownNullableUShort),
         new PropertyMetadata((ushort)0));
      public ushort TickDefaultValue { get => (ushort)GetValue(TickDefaultValueProperty); set => SetValue(TickDefaultValueProperty, value); }

      public override void OnApplyTemplate() {
         var tick_up_down = this.assert_template_child<TickUpDown>("PART_tick_up_down");
         tick_up_down.TickUpCommand = new DelegateCommand(_tick_up);
         tick_up_down.TickDownCommand = new DelegateCommand(_tick_down);
      }

      void _tick_up() {
         ushort? v = Value;
         if (v.HasValue) {
            int result = v.Value + Increment;
            Value = result < ushort.MaxValue ? (ushort)result : ushort.MaxValue;
         } else Value = TickDefaultValue;
      }

      void _tick_down() {
         ushort? v = Value;
         if (v.HasValue) {
            // do not tick across 0
            Value = v.Value > Increment ?  (ushort)(v.Value - Increment) : (ushort)0;
         } else Value = TickDefaultValue;
      }
   }
}