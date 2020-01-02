using RGrid.Utility;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace RGrid.Controls {
   public partial class TickUpDown : ContentControl, System.ComponentModel.ISupportInitialize {
      static TickUpDown() =>
         DefaultStyleKeyProperty.OverrideMetadata(typeof(TickUpDown), new FrameworkPropertyMetadata(typeof(TickUpDown)));

      protected override void OnPreviewMouseWheel(MouseWheelEventArgs e) {
         if (ActivateOnMouseWheel) {
            if (e.Delta > 0) {
               TickUpCommand.TryExecuteIfCan(TickUpParameter);
            }
            else {
               TickDownCommand.TryExecuteIfCan(TickDownParameter);
            }
            e.Handled = true;
         }
         else {
            base.OnMouseWheel(e);
         }
      }

      protected override void OnPreviewKeyDown(KeyEventArgs e) {
         if (EnableArrowKeyTick) {
            switch (Keyboard.Modifiers) {
               case ModifierKeys.None: {
                     switch (e.Key) {
                        case Key.Up:
                           TickUpCommand.TryExecuteIfCan(TickUpParameter);
                           e.Handled = true;
                           break;
                        case Key.Down:
                           TickDownCommand.TryExecuteIfCan(TickDownParameter);
                           e.Handled = true;
                           break;
                     }
                  }
                  break;
               case ModifierKeys.Alt: {
                     // see: https://stackoverflow.com/questions/3099472/previewkeydown-is-not-seeing-alt-modifiers
                     switch (e.SystemKey) {
                        case Key.Up:
                           TickUpCommand.TryExecuteIfCan(TickUpAltParameter);
                           e.Handled = true;
                           break;
                        case Key.Down:
                           TickDownCommand.TryExecuteIfCan(TickDownAltParameter);
                           e.Handled = true;
                           break;
                     }
                  }
                  break;
            }
         }
         base.OnPreviewKeyDown(e);
      }

      public bool EnableArrowKeyTick { get; set; } = true;

      #region TickCommand
      public ICommand TickCommand {
         get { return (ICommand)GetValue(TickCommandProperty); }
         set { SetValue(TickCommandProperty, value); }
      }
      public static readonly DependencyProperty TickCommandProperty =
          DependencyProperty.Register("TickCommand", typeof(ICommand), typeof(TickUpDown), new PropertyMetadata(new PropertyChangedCallback(on_tick_command_changed)));

      private static void on_tick_command_changed(DependencyObject d, DependencyPropertyChangedEventArgs e) {
         TickUpDown tud = d as TickUpDown;
         if (tud != null) {
            tud.TickUpCommand = (ICommand)e.NewValue;
            tud.TickDownCommand = (ICommand)e.NewValue;
         }
      }
      #endregion

      #region TickUpCommand
      public ICommand TickUpCommand {
         get { return (ICommand)GetValue(TickUpCommandProperty); }
         set { SetValue(TickUpCommandProperty, value); }
      }

      public static readonly DependencyProperty TickUpCommandProperty =
          DependencyProperty.Register("TickUpCommand", typeof(ICommand), typeof(TickUpDown), new PropertyMetadata(null));
      #endregion

      #region TickDownCommand
      public ICommand TickDownCommand {
         get { return (ICommand)GetValue(TickDownCommandProperty); }
         set { SetValue(TickDownCommandProperty, value); }
      }

      public static readonly DependencyProperty TickDownCommandProperty =
          DependencyProperty.Register("TickDownCommand", typeof(ICommand), typeof(TickUpDown), new PropertyMetadata(null));
      #endregion

      #region TickUpParameter
      public object TickUpParameter {
         get { return GetValue(TickUpParameterProperty); }
         set { SetValue(TickUpParameterProperty, value); }
      }

      public static readonly DependencyProperty TickUpParameterProperty =
          DependencyProperty.Register("TickUpParameter", typeof(object), typeof(TickUpDown), new PropertyMetadata(OnTickUpParameterChanged));

      static void OnTickUpParameterChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) =>
         d.CoerceValue(TickUpAltParameterProperty);
      #endregion

      #region TickDownParameter
      public object TickDownParameter {
         get => GetValue(TickDownParameterProperty);
         set => SetValue(TickDownParameterProperty, value);
      }

      public static readonly DependencyProperty TickDownParameterProperty =
          DependencyProperty.Register("TickDownParameter", typeof(object), typeof(TickUpDown), new PropertyMetadata(OnTickDownParameterChanged));

      static void OnTickDownParameterChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) =>
         d.CoerceValue(TickDownAltParameterProperty);
      #endregion

      #region TickUpAltParameter
      public object TickUpAltParameter {
         get { return GetValue(TickUpAltParameterProperty); }
         set { SetValue(TickUpAltParameterProperty, value); }
      }

      public static readonly DependencyProperty TickUpAltParameterProperty = DependencyProperty.Register(
         nameof(TickUpAltParameter), typeof(object), typeof(TickUpDown), new PropertyMetadata(null, null, CoerceTickUpAltParameter));

      static object CoerceTickUpAltParameter(DependencyObject d, object baseValue) =>
         baseValue ?? d.GetValue(TickUpParameterProperty);
      #endregion

      #region TickDownAlt
      public object TickDownAltParameter {
         get { return GetValue(TickDownAltParameterProperty); }
         set { SetValue(TickDownAltParameterProperty, value); }
      }

      public static readonly DependencyProperty TickDownAltParameterProperty = DependencyProperty.Register(
         nameof(TickDownAltParameter), typeof(object), typeof(TickUpDown), new PropertyMetadata(null, null, CoerceTickDownAltParameter));

      static object CoerceTickDownAltParameter(DependencyObject d, object baseValue) =>
         baseValue ?? d.GetValue(TickDownParameterProperty);
      #endregion

      #region ActivateOnMouseWheel
      public bool ActivateOnMouseWheel {
         get { return (bool)GetValue(ActivateOnMouseWheelProperty); }
         set { SetValue(ActivateOnMouseWheelProperty, value); }
      }

      public static readonly DependencyProperty ActivateOnMouseWheelProperty =
          DependencyProperty.Register("ActivateOnMouseWheel", typeof(bool), typeof(TickUpDown), new PropertyMetadata(true));
      #endregion
   }
}