using Disposable.Extensions;
using Disposable.Extensions.Utilities;
using RGrid.Utility;
using RGrid.WPF;
using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;

namespace RGrid.Controls.Filter {
   /// <summary>
   /// Interaction logic for UShortMinMaxFilterControl.xaml
   /// </summary>
   class UShortMinMaxFilterControl : Control {
		static UShortMinMaxFilterControl() =>
			DefaultStyleKeyProperty.OverrideMetadata(typeof(UShortMinMaxFilterControl), new FrameworkPropertyMetadata(typeof(UShortMinMaxFilterControl)));

      readonly BindingUpdateGroup
        _min_text_update = new BindingUpdateGroup(),
        _max_text_update = new BindingUpdateGroup();
      IDisposable _hooks;

      public UShortMinMaxFilterControl() {
         _min_text_update.Properties.Add(TextBox.TextProperty);
         _max_text_update.Properties.Add(TextBox.TextProperty);
         BindingOperations.SetBinding(_min_text_update, BindingUpdateGroup.SourceProperty, new Binding(nameof(RequestBindingUpdateSource)) { Source = this });
         BindingOperations.SetBinding(_max_text_update, BindingUpdateGroup.SourceProperty, new Binding(nameof(RequestBindingUpdateSource)) { Source = this });
      }

      public static readonly DependencyProperty MinimumProperty = DependencyProperty.Register(nameof(Minimum), typeof(ushort?), typeof(UShortMinMaxFilterControl),
			new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault));
		public ushort? Minimum { get => (ushort?)GetValue(MinimumProperty); set => SetValue(MinimumProperty, value); }

		public static readonly DependencyProperty MaximumProperty = DependencyProperty.Register(nameof(Maximum), typeof(ushort?), typeof(UShortMinMaxFilterControl),
			new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault));
		public ushort? Maximum { get => (ushort?)GetValue(MaximumProperty); set => SetValue(MaximumProperty, value); }

      public static readonly DependencyProperty RequestBindingUpdateSourceProperty = DependencyProperty.Register(
         nameof(RequestBindingUpdateSource), typeof(IRequestBindingSourceUpdate), typeof(UShortMinMaxFilterControl));
      public IRequestBindingSourceUpdate RequestBindingUpdateSource { get => GetValue(RequestBindingUpdateSourceProperty) as IRequestBindingSourceUpdate; set => SetValue(RequestBindingUpdateSourceProperty, value); }

      public override void OnApplyTemplate() {
         DisposableUtils.Dispose(ref _hooks);
         var min_tick = this.assert_template_child<TickUpDownNullableUShort>("PART_Minimum_Tick");
         var max_tick = this.assert_template_child<TickUpDownNullableUShort>("PART_Maximum_Tick");
         set_max_target();
         set_max_target();
         _hooks = DisposableFactory.Create(
            min_tick.SubscribeLoaded((s, e) => set_min_target()),
            max_tick.SubscribeLoaded((s, e) => set_max_target())
         );

         void set_min_target() =>
            _min_text_update.Target = min_tick.descendants_of_type<TextBox>().FirstOrDefault();

         void set_max_target() =>
            _max_text_update.Target = max_tick.descendants_of_type<TextBox>().FirstOrDefault();
      }
   }
}