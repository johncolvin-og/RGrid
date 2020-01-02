using RGrid.Utility;
using RGrid.WPF;
using System;
using System.Windows;
using System.Windows.Controls;

namespace RGrid.Controls {
   class TickUpDownInt : Control {
      static TickUpDownInt() =>
         DefaultStyleKeyProperty.OverrideMetadata(typeof(TickUpDownInt), new FrameworkPropertyMetadata(typeof(TickUpDownInt)));

      public static readonly DependencyProperty MinimumProperty =
         WPFHelper.create_dp<int, TickUpDownInt>(
            nameof(Minimum),
            (o, v) => o.CoerceValue(ValueProperty),
            int.MinValue);

      public int Minimum {
         get => (int)GetValue(MinimumProperty);
         set => SetValue(MinimumProperty, value);
      }

      public static readonly DependencyProperty MaximumProperty =
        WPFHelper.create_dp<int, TickUpDownInt>(
           nameof(Maximum),
           (o, v) => o.CoerceValue(ValueProperty),
           int.MaxValue);

      public int Maximum {
         get => (int)GetValue(MaximumProperty);
         set => SetValue(MaximumProperty, value);
      }

      public static readonly DependencyProperty ValueProperty =
         WPFHelper.create_dp<int, TickUpDownInt>(
            nameof(Value),
            FrameworkPropertyMetadataOptions.BindsTwoWayByDefault,
            (o, v) => MathUtils.within_range(o.Minimum, o.Maximum, v),
            0);

      public int Value {
         get => (int)GetValue(ValueProperty);
         set => SetValue(ValueProperty, value);
      }

      public static readonly DependencyProperty IncrementProperty = DependencyProperty.Register("Increment", typeof(int), typeof(TickUpDownInt), new FrameworkPropertyMetadata(1));

      public int Increment { get { return (int)GetValue(IncrementProperty); } set { SetValue(IncrementProperty, value); } }

      public override void OnApplyTemplate() {
         var tick_up_down = GetTemplateChild("PART_tick_up_down") as TickUpDown;
         if (tick_up_down == null) throw new InvalidOperationException($"{GetType().FullName} Template must have a {typeof(TickUpDown).FullName} named PART_tick_up_down");
         tick_up_down.TickUpCommand = new DelegateCommand(_tick_up);
         tick_up_down.TickDownCommand = new DelegateCommand(_tick_down);
      }

      private void _tick_up() { Value += Increment; }
      private void _tick_down() { Value -= Increment; }
   }
}