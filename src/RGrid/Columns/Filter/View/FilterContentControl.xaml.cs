using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace RGrid.Controls.Filter {
   /// <summary>
   /// Interaction logic for FilterContentControl.xaml
   /// </summary>
   class FilterContentControl : ContentControl {
      static FilterContentControl() =>
         DefaultStyleKeyProperty.OverrideMetadata(typeof(FilterContentControl), new FrameworkPropertyMetadata(typeof(FilterContentControl)));

      #region ApplyCommand
      public static readonly DependencyProperty ApplyCommandProperty = DependencyProperty.Register(
         nameof(ApplyCommand), typeof(ICommand), typeof(FilterContentControl));

      public ICommand ApplyCommand { get => GetValue(ApplyCommandProperty) as ICommand; set => SetValue(ApplyCommandProperty, value); }
      #endregion

      #region ApplyCommandParameter
      public static readonly DependencyProperty ApplyCommandParameterProperty = DependencyProperty.Register(
         nameof(ApplyCommandParameter), typeof(object), typeof(FilterContentControl));

      public object ApplyCommandParameter { get => GetValue(ApplyCommandParameterProperty); set => SetValue(ApplyCommandParameterProperty, value); }
      #endregion

      #region ClearCommand
      public static readonly DependencyProperty ClearCommandProperty = DependencyProperty.Register(
         nameof(ClearCommand), typeof(ICommand), typeof(FilterContentControl));

      public ICommand ClearCommand { get => GetValue(ClearCommandProperty) as ICommand; set => SetValue(ClearCommandProperty, value); }
      #endregion

      #region ClearCommandParameter
      public static readonly DependencyProperty ClearCommandParameterProperty = DependencyProperty.Register(
         nameof(ClearCommandParameter), typeof(object), typeof(FilterContentControl));

      public object ClearCommandParameter { get => GetValue(ClearCommandParameterProperty); set => SetValue(ClearCommandParameterProperty, value); }
      #endregion
   }
}