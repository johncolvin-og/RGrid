using RGrid.WPF;
using System;
using System.Windows;
using System.Windows.Controls;

namespace RGrid.Controls
{
   public class TickUpDownUInt : Control
   {
      static TickUpDownUInt() {
         DefaultStyleKeyProperty.OverrideMetadata(typeof(TickUpDownUInt), new FrameworkPropertyMetadata(typeof(TickUpDownUInt)));
      }

      public static readonly DependencyProperty ValueProperty = DependencyProperty.Register("Value", typeof(uint), typeof(TickUpDownUInt), new FrameworkPropertyMetadata(0u, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault));
      public static readonly DependencyProperty IncrementProperty = DependencyProperty.Register("Increment", typeof(uint), typeof(TickUpDownUInt), new FrameworkPropertyMetadata(1u));

      public uint Value { get { return (uint)GetValue(ValueProperty); } set { SetValue(ValueProperty, value); } }
      public uint Increment { get { return (uint)GetValue(IncrementProperty); } set { SetValue(IncrementProperty, value); } }

      public override void OnApplyTemplate() {
         var tick_up_down = GetTemplateChild("PART_tick_up_down") as TickUpDown;
         if (tick_up_down == null) throw new InvalidOperationException($"{GetType().FullName} Template must have a {typeof(TickUpDown).FullName} named PART_tick_up_down");
         tick_up_down.TickUpCommand = new DelegateCommand(_tick_up);
         tick_up_down.TickDownCommand = new DelegateCommand(_tick_down);
      }

      private void _tick_up() { Value += Increment; }
      private void _tick_down() {
         // do not tick across 0
         uint curr_val = Value;
         uint new_val = curr_val - Increment;
         if (new_val < curr_val) Value = new_val;
      }
   }
}