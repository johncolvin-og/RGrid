using RGrid.WPF;
using System.Windows;
using System.Windows.Controls;

namespace RGrid.Controls {
   public class TickUpDownNullableUInt : Control {
		static TickUpDownNullableUInt() =>
			DefaultStyleKeyProperty.OverrideMetadata(typeof(TickUpDownNullableUInt), new FrameworkPropertyMetadata(typeof(TickUpDownNullableUInt)));

		public static readonly DependencyProperty ValueProperty = DependencyProperty.Register("Value", typeof(uint?), typeof(TickUpDownNullableUInt),
         new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault));
		public uint? Value { get => GetValue(ValueProperty) as uint?; set => SetValue(ValueProperty, value); }

		public static readonly DependencyProperty IncrementProperty = DependencyProperty.Register("Increment", typeof(uint), typeof(TickUpDownNullableUInt),
         new PropertyMetadata(1u));
		public uint Increment { get { return (uint)GetValue(IncrementProperty); } set { SetValue(IncrementProperty, value); } }

      public static readonly DependencyProperty TickDefaultValueProperty = DependencyProperty.Register(nameof(TickDefaultValue), typeof(uint), typeof(TickUpDownNullableUInt),
         new PropertyMetadata(0u));
      public uint TickDefaultValue { get => (uint)GetValue(TickDefaultValueProperty); set => SetValue(TickDefaultValueProperty, value); }

      public static readonly DependencyProperty CommitOnEnterProperty = DependencyProperty.Register(
         nameof(CommitOnEnter), typeof(bool), typeof(TickUpDownNullableUInt), new PropertyMetadata(false));

      public bool CommitOnEnter { get => (bool)GetValue(CommitOnEnterProperty); set => SetValue(CommitOnEnterProperty, value); }

      public override void OnApplyTemplate() {
         var tick_up_down = this.assert_template_child<TickUpDown>("PART_tick_up_down");
			tick_up_down.TickUpCommand = new DelegateCommand(_tick_up);
			tick_up_down.TickDownCommand = new DelegateCommand(_tick_down);
		}

		void _tick_up() {
			uint? v = Value;
         Value = v.HasValue ? v.Value + Increment : TickDefaultValue;
		}

		void _tick_down() {
			uint? v = Value;
         if (v.HasValue) {
            // do not tick across 0
            Value = v.Value > Increment ? v.Value - Increment : 0u;
         } else Value = TickDefaultValue;
		}
	}
}