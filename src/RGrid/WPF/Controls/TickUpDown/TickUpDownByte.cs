using RGrid.WPF;
using System;
using System.Windows;
using System.Windows.Controls;

namespace RGrid.Controls
{
   public class TickUpDownByte : Control
   {
      static TickUpDownByte() {
         DefaultStyleKeyProperty.OverrideMetadata(typeof(TickUpDownByte), new FrameworkPropertyMetadata(typeof(TickUpDownByte)));
      }

      public static readonly DependencyProperty ValueProperty = DependencyProperty.Register("Value", typeof(byte), typeof(TickUpDownByte), new FrameworkPropertyMetadata((byte)0, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault));
      public static readonly DependencyProperty IncrementProperty = DependencyProperty.Register("Increment", typeof(byte), typeof(TickUpDownByte), new FrameworkPropertyMetadata((byte)1));

      public byte Value { get { return (byte)GetValue(ValueProperty); } set { SetValue(ValueProperty, value); } }
      public byte Increment { get { return (byte)GetValue(IncrementProperty); } set { SetValue(IncrementProperty, value); } }

      public override void OnApplyTemplate() {
         var tick_up_down = GetTemplateChild("PART_tick_up_down") as TickUpDown;
         if (tick_up_down == null)
            throw new InvalidOperationException($"{GetType().FullName} Template must have a {typeof(TickUpDown).FullName} named PART_tick_up_down");
         tick_up_down.TickUpCommand = new DelegateCommand(_tick_up);
         tick_up_down.TickDownCommand = new DelegateCommand(_tick_down);
      }

      private void _tick_up() { Value += Increment; }
      private void _tick_down() { Value -= Increment; }
   }
}