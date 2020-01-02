using RGrid.Utility;
using System;
using System.Windows;
using System.Windows.Input;

namespace RGrid.WPF {
   static class Behavior {
      #region EatMouseDown
      public static readonly DependencyProperty EatMouseDownProperty =
         DependencyProperty.RegisterAttached("EatMouseDown", typeof(bool), typeof(Behavior), new PropertyMetadata(false, OnEatMouseDownChanged));

      static void OnEatMouseDownChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) {
         if (d is FrameworkElement el) {
            if ((bool)e.NewValue) {
               el.MouseDown += _eat_mouse_down_handler;
            } else {
               el.MouseDown -= _eat_mouse_down_handler;
            }
         }
      }

      static void _eat_mouse_down_handler(object sender, MouseButtonEventArgs e) => e.Handled = true;

      public static bool GetEatMouseDown(DependencyObject d) => (bool)d.GetValue(EatMouseDownProperty);
      public static void SetEatMouseDown(DependencyObject d, bool value) => d.SetValue(EatMouseDownProperty, value);
      #endregion

      #region BindingUpdateGroups
      public static readonly DependencyProperty BindingUpdateGroupsProperty =
         DependencyProperty.RegisterAttached("BindingUpdateGroups", typeof(BindingUpdateGroupCollection), typeof(Behavior), new PropertyMetadata(null));

      public static BindingUpdateGroupCollection GetBindingUpdateGroups(DependencyObject d) => d.GetValue(BindingUpdateGroupsProperty) as BindingUpdateGroupCollection;
      public static void SetBindingUpdateGroups(DependencyObject d, BindingUpdateGroupCollection value) => d.SetValue(BindingUpdateGroupsProperty, value);
      #endregion

      #region AutoFocusTargetProvider
      public static readonly DependencyProperty AutoFocusTargetProviderProperty = DependencyProperty.RegisterAttached("AutoFocusTargetProvider", typeof(bool),
         typeof(Behavior), new PropertyMetadata(OnAutoFocusTargetProviderChanged));

      static void OnAutoFocusTargetProviderChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) {
         if (e.NewValue is IAutoFocusTargetProvider aftp) {
            var el = ExceptionAssert.Argument.Is<FrameworkElement>(d, nameof(d));
            d.SetValue(AutoFocusTargetProviderLifeDependencyPropertyKey, new AutoFocusTargetProviderLifeDependency(el, aftp));
         } else
            d.SetValue(AutoFocusTargetProviderLifeDependencyPropertyKey, null);
      }

      public static bool GetAutoFocusTargetProvider(DependencyObject d) => (bool)d.GetValue(AutoFocusTargetProviderProperty);
      public static void SetAutoFocusTargetProvider(DependencyObject d, bool value) => d.SetValue(AutoFocusTargetProviderProperty, value);
      #endregion

      #region AutoFocusTargetProviderLifeDependency (ReadOnly)
      static readonly DependencyPropertyKey AutoFocusTargetProviderLifeDependencyPropertyKey = DependencyProperty.RegisterAttachedReadOnly("AutoFocusTargetProviderLifeDependency",
         typeof(FrameworkElementLifeDependency), typeof(Behavior), new PropertyMetadata(FrameworkElementLifeDependencyHelper.DisposeOldInitializeNew));

      public static readonly DependencyProperty AutoFocusTargetProviderLifeDependencyProperty = AutoFocusTargetProviderLifeDependencyPropertyKey.DependencyProperty;

      public static FrameworkElementLifeDependency GetAutoFocusTargetProviderLifeDependency(DependencyObject d) => d.GetValue(AutoFocusTargetProviderLifeDependencyProperty) as FrameworkElementLifeDependency;
      public static void SetAutoFocusTargetProviderLifeDependency(DependencyObject d, FrameworkElementLifeDependency value) => d.SetValue(AutoFocusTargetProviderLifeDependencyPropertyKey, value);

      class AutoFocusTargetProviderLifeDependency : FrameworkElementLifeDependency {
         readonly IAutoFocusTargetProvider _focus_target_provider;

         public AutoFocusTargetProviderLifeDependency(FrameworkElement root, IAutoFocusTargetProvider focus_target_provider) : base(root) =>
            _focus_target_provider = focus_target_provider;

         protected override void OnLoaded() {
            var el = _focus_target_provider.Target;
            if (el != null) {
               if (_focus_target_provider.Focus)
                  el.Focus();
               if (_focus_target_provider.FocusKeyboard)
                  Keyboard.Focus(el);
               if (_focus_target_provider.CaptureMouse)
                  el.CaptureMouse();
            }
         }

         protected override void OnUnloaded() {
            var el = _focus_target_provider.Target;
            if (el != null) {
               if (_focus_target_provider.ReleaseKeyboard) {
                  ReferenceEquals(Keyboard.FocusedElement, el);
                  Keyboard.ClearFocus();
               }
               if (_focus_target_provider.ReleaseMouse)
                  el.ReleaseMouseCapture();
            }
         }
      }
      #endregion
   }
}