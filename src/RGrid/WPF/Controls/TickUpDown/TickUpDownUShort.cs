using RGrid.WPF;
using System;
using System.Windows;
using System.Windows.Controls;

namespace RGrid.Controls
{
	public class TickUpDownUShort : Control
	{
		static TickUpDownUShort() {
			DefaultStyleKeyProperty.OverrideMetadata(typeof(TickUpDownUShort), new FrameworkPropertyMetadata(typeof(TickUpDownUShort)));
		}

		public static readonly DependencyProperty ValueProperty = DependencyProperty.Register("Value", typeof(ushort), typeof(TickUpDownUShort), new FrameworkPropertyMetadata((ushort)0, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault));
		public static readonly DependencyProperty IncrementProperty = DependencyProperty.Register("Increment", typeof(ushort), typeof(TickUpDownUShort), new FrameworkPropertyMetadata((ushort)1));

		public ushort Value { get { return (ushort)GetValue(ValueProperty); } set { SetValue(ValueProperty, value); } }
		public ushort Increment { get { return (ushort)GetValue(IncrementProperty); } set { SetValue(IncrementProperty, value); } }

		public override void OnApplyTemplate() {
			var tick_up_down = GetTemplateChild("PART_tick_up_down") as TickUpDown;
         if (tick_up_down == null)
            throw new InvalidOperationException($"{GetType().FullName} Template must have a {typeof(TickUpDown).FullName} named PART_tick_up_down");
			tick_up_down.TickUpCommand = new DelegateCommand(_tick_up);
			tick_up_down.TickDownCommand = new DelegateCommand(_tick_down);
		}

		private void _tick_up() { Value += Increment; }
		private void _tick_down() {
			// do not tick across 0
			ushort curr_val = Value;
			ushort new_val = (ushort)(curr_val - Increment);
			if (new_val < curr_val) Value = new_val;
		}
	}
}