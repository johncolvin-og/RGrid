using RGrid.WPF;
using System;
using System.Windows;
using System.Windows.Controls;

namespace RGrid.Controls {
   public class TickUpDownULong : Control {
      static TickUpDownULong() {
         DefaultStyleKeyProperty.OverrideMetadata(typeof(TickUpDownULong), new FrameworkPropertyMetadata(typeof(TickUpDownULong)));
      }

      public static readonly DependencyProperty ValueProperty = DependencyProperty.Register("Value", typeof(ulong), typeof(TickUpDownULong), new FrameworkPropertyMetadata((ulong)0, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault));
      public static readonly DependencyProperty IncrementProperty = DependencyProperty.Register("Increment", typeof(ulong), typeof(TickUpDownULong), new FrameworkPropertyMetadata((ulong)1));

      public ulong Value { get { return (ulong)GetValue(ValueProperty); } set { SetValue(ValueProperty, value); } }
      public ulong Increment { get { return (ulong)GetValue(IncrementProperty); } set { SetValue(IncrementProperty, value); } }

      public override void OnApplyTemplate() {
         if (!(GetTemplateChild("PART_tick_up_down") is TickUpDown tick_up_down))
            throw new InvalidOperationException($"{GetType().FullName} Template must have a {typeof(TickUpDown).FullName} named PART_tick_up_down");
         tick_up_down.TickUpCommand = new DelegateCommand(_tick_up);
         tick_up_down.TickDownCommand = new DelegateCommand(_tick_down);
      }

      private void _tick_up() { Value += Increment; }
      private void _tick_down() {
         // do not tick across 0
         ulong curr_val = Value;
         ulong new_val = curr_val - Increment;
         if (new_val < curr_val) Value = new_val;
      }
   }
}