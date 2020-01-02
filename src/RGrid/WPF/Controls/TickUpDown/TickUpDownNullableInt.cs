using RGrid.WPF;
using System.Windows;
using System.Windows.Controls;

namespace RGrid.Controls {
   public class TickUpDownNullableInt : Control {
      static TickUpDownNullableInt() =>
         DefaultStyleKeyProperty.OverrideMetadata(typeof(TickUpDownNullableInt), new FrameworkPropertyMetadata(typeof(TickUpDownNullableInt)));

      public static readonly DependencyProperty ValueProperty = DependencyProperty.Register("Value", typeof(int?), typeof(TickUpDownNullableInt),
         new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault));
      public int? Value { get => GetValue(ValueProperty) as int?; set => SetValue(ValueProperty, value); }

      public static readonly DependencyProperty IncrementProperty = DependencyProperty.Register("Increment", typeof(int), typeof(TickUpDownNullableInt),
         new PropertyMetadata(1));
      public int Increment { get { return (int)GetValue(IncrementProperty); } set { SetValue(IncrementProperty, value); } }

      public static readonly DependencyProperty TickDefaultValueProperty = DependencyProperty.Register(nameof(TickDefaultValue), typeof(int), typeof(TickUpDownNullableInt),
         new PropertyMetadata(0));
      public int TickDefaultValue { get => (int)GetValue(TickDefaultValueProperty); set => SetValue(TickDefaultValueProperty, value); }

      public override void OnApplyTemplate() {
         var tick_up_down = this.assert_template_child<TickUpDown>("PART_tick_up_down");
         tick_up_down.TickUpCommand = new DelegateCommand(_tick_up);
         tick_up_down.TickDownCommand = new DelegateCommand(_tick_down);
      }

      void _tick_up() {
         int? v = Value;
         Value = v.HasValue ? v.Value + Increment : TickDefaultValue;
      }

      void _tick_down() {
         int? v = Value;
         Value = v.HasValue ? v.Value - Increment : TickDefaultValue;
      }
   }
}