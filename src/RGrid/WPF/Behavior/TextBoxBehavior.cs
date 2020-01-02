using RGrid.Utility;
using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;

namespace RGrid.WPF {
   static class TextBoxBehavior {
      #region RoutedCommands/Handlers
      public static readonly RoutedCommand
         ReleaseCaptureCommand = new RoutedCommand(),
         SelectAllCommand = new RoutedCommand();

      public static readonly ExecutedRoutedEventHandler SelectAllCommandExecutedHandler =
         new ExecutedRoutedEventHandler((s, e) => (e.OriginalSource as TextBox ?? e.Source as TextBox ?? s as TextBox)?.SelectAll());
      #endregion

      #region SelectAllOnFocus

      public static readonly DependencyProperty SelectAllOnFocusProperty = DependencyProperty.RegisterAttached("SelectAllOnFocus", typeof(bool), typeof(TextBoxBehavior), new PropertyMetadata(false, OnSelectAllOnFocusChanged));
      private static void OnSelectAllOnFocusChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) {
         var text_box = (TextBox)d;
         if ((bool)e.NewValue) text_box.GotFocus += select_all_GotFocusHandler;
         else text_box.GotFocus -= select_all_GotFocusHandler;
      }
      private static void select_all_GotFocusHandler(object sender, RoutedEventArgs e) { ((TextBox)sender).SelectAll(); }
      public static bool GetSelectAllOnFocus(TextBox element) { return (bool)element.GetValue(SelectAllOnFocusProperty); }
      public static void SetSelectAllOnFocus(TextBox element, bool value) { element.SetValue(SelectAllOnFocusProperty, value); }

      #endregion

      #region TraverseOnEnter

      public static readonly DependencyProperty TraverseOnEnterProperty = DependencyProperty.RegisterAttached("TraverseOnEnter", typeof(bool), typeof(TextBoxBehavior), new PropertyMetadata(false, OnTraverseOnEnterChanged));
      private static void OnTraverseOnEnterChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) {
         var text_box = (TextBox)d;
         if ((bool)e.NewValue) text_box.PreviewKeyDown += commit_on_enter_PreviewKeyDownHandler;
         else text_box.PreviewKeyDown -= commit_on_enter_PreviewKeyDownHandler;
      }
      private static void commit_on_enter_PreviewKeyDownHandler(object sender, System.Windows.Input.KeyEventArgs e) {
         if (e.Key == Key.Enter) ((TextBox)sender).MoveFocus(new TraversalRequest(FocusNavigationDirection.Next));
      }
      public static bool GetTraverseOnEnter(TextBox target) { return (bool)target.GetValue(TraverseOnEnterProperty); }
      public static void SetTraverseOnEnter(TextBox target, bool value) { target.SetValue(TraverseOnEnterProperty, value); }

      #endregion

      #region CommitOnEnter
      public static readonly DependencyProperty CommitOnEnterProperty =
         DependencyProperty.RegisterAttached("CommitOnEnter", typeof(bool), typeof(TextBoxBehavior), new PropertyMetadata(false, commit_on_enter_changed));

      static void commit_on_enter_changed(DependencyObject d, DependencyPropertyChangedEventArgs e) {
         var tb = (TextBox)d;
         if ((bool)e.NewValue) tb.KeyDown += tb_keydown;
         else tb.KeyDown -= tb_keydown;
      }

      static void tb_keydown(object sender, KeyEventArgs e) {
         if (e.Key == Key.Enter) {
            var binding = BindingOperations.GetBindingExpression(sender as DependencyObject, TextBox.TextProperty);
            binding.UpdateSource();
         }
      }

      public static bool GetCommitOnEnter(DependencyObject obj) => (bool)obj.GetValue(CommitOnEnterProperty);
      public static void SetCommitOnEnter(DependencyObject obj, bool value) => obj.SetValue(CommitOnEnterProperty, value);
      #endregion

      #region CancelOnEnter

      public static readonly DependencyProperty CancelOnEscapeProperty =
          DependencyProperty.RegisterAttached("CancelOnEscape", typeof(bool), typeof(TextBoxBehavior), new PropertyMetadata(false, cancel_on_escape_changed));

      private static void cancel_on_escape_changed(DependencyObject d, DependencyPropertyChangedEventArgs e) {
         var tb = (TextBox)d;
         if ((bool)e.NewValue) {
            tb.KeyDown += tbx_keydown;
         }
         else {
            tb.KeyDown += tbx_keydown;
         }
      }

      static void tbx_keydown(object sender, KeyEventArgs e) {
         if (e.Key == Key.Escape) {
            abort_edit(sender as DependencyObject);
         }
      }

      // Copy the binding value back and forth to generate a set to the source property with the original value
      private static void abort_edit(DependencyObject sender) {
         if (sender != null) {
            var binding = BindingOperations.GetBindingExpression(sender, TextBox.TextProperty);
            binding?.UpdateTarget();
            binding?.UpdateSource();
         }
      }

      public static bool GetCancelOnEscape(DependencyObject obj) => (bool)obj.GetValue(CancelOnEscapeProperty);
      public static void SetCancelOnEscape(DependencyObject obj, bool value) => obj.SetValue(CancelOnEscapeProperty, value);

      #endregion

      #region TryCancelOnFocusLost
      public static readonly DependencyProperty TryCancelOnFocusLostProperty =
          DependencyProperty.RegisterAttached("TryCancelOnFocusLost", typeof(bool), typeof(TextBoxBehavior), new PropertyMetadata(false, cancel_on_focus_lost_changed));

      private static void cancel_on_focus_lost_changed(DependencyObject d, DependencyPropertyChangedEventArgs e) {
         var tb = (TextBox)d;
         if ((bool)e.NewValue) {
            tb.LostFocus += tb_lostfocus;
         }
         else {
            tb.LostFocus += tb_lostfocus;
         }
      }

      static void tb_lostfocus(object sender, EventArgs e) {
         abort_edit(sender as DependencyObject);
      }

      public static bool GetTryCancelOnFocusLost(DependencyObject obj) => (bool)obj.GetValue(TryCancelOnFocusLostProperty);
      public static void SetTryCancelOnFocusLost(DependencyObject obj, bool value) => obj.SetValue(TryCancelOnFocusLostProperty, value);

      #endregion


      #region ReleaseOnEscape
      public static readonly DependencyProperty ReleaseOnEscapeProperty = DependencyProperty.RegisterAttached(
         "ReleaseOnEscape", typeof(bool), typeof(TextBoxBehavior), new PropertyMetadata(false, OnReleaseOnEscapeChanged));

      static void OnReleaseOnEscapeChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) {
         var tb = (TextBox)d;
         if ((bool)e.NewValue) {
            tb.InputBindings.Add(new KeyBinding(ReleaseCaptureCommand, Key.Escape, ModifierKeys.None));
            tb.CommandBindings.Add(new CommandBinding(ReleaseCaptureCommand, (s, cmd_e) => {
               tb.ReleaseMouseCapture();
               Keyboard.ClearFocus();
            }));
         }
         else {
            tb.ClearKeyBindings(Key.Escape, ModifierKeys.None);
            tb.ClearCommandBindings(ReleaseCaptureCommand);
         }
      }

      public static bool GetReleaseOnEscape(DependencyObject obj) => (bool)obj.GetValue(ReleaseOnEscapeProperty);
      public static void SetReleaseOnEscape(DependencyObject obj, bool value) => obj.SetValue(ReleaseOnEscapeProperty, value);
      #endregion

      #region FocusOnLoaded
      public static readonly DependencyProperty FocusOnLoadedProperty =
         DependencyProperty.RegisterAttached("FocusOnLoaded", typeof(bool), typeof(TextBoxBehavior), new PropertyMetadata(false, OnFocusOnLoadedChanged));

      static void OnFocusOnLoadedChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) {
         var target = (FrameworkElement)d;
         if ((bool)e.NewValue) target.Loaded += _target_Loaded;
         else target.Loaded -= _target_Loaded;
      }

      static void _target_Loaded(object sender, RoutedEventArgs e) {
         var target = (FrameworkElement)sender;
         Keyboard.Focus(target);
      }
      public static bool GetFocusOnLoaded(DependencyObject d) => (bool)d.GetValue(FocusOnLoadedProperty);
      public static void SetFocusOnLoaded(DependencyObject d, bool value) => d.SetValue(FocusOnLoadedProperty, value);
      #endregion

      #region EnableArrowKeyTraverse
      public static readonly DependencyProperty EnableArrowKeyTraverseProperty = DependencyProperty.RegisterAttached(
         "EnableArrowKeyTraverse", typeof(bool), typeof(TextBoxBehavior), new PropertyMetadata(false, OnEnableArrowKeyTraverseChanged));

      static void OnEnableArrowKeyTraverseChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) {
         if (d is UIElement el) {
            if ((bool)e.NewValue)
               el.PreviewKeyDown += _enable_arrow_key_traverse_key_down;
            else
               el.PreviewKeyDown -= _enable_arrow_key_traverse_key_down;
         }
      }

      static void _enable_arrow_key_traverse_key_down(object sender, KeyEventArgs e) {
         if (Keyboard.Modifiers == ModifierKeys.Control) {
            switch (e.Key) {
               case Key.Left: {
                     var tb = (TextBox)sender;
                     string txt = tb.Text;
                     if (string.IsNullOrEmpty(txt) || tb.CaretIndex == 0) {
                        if (!tb.MoveFocus(new TraversalRequest(FocusNavigationDirection.Left)))
                           tb.MoveFocus(new TraversalRequest(FocusNavigationDirection.Previous));
                        e.Handled = true;
                     }
                  }
                  break;
               case Key.Right: {
                     var tb = (TextBox)sender;
                     string txt = tb.Text;
                     if (string.IsNullOrEmpty(txt) || tb.CaretIndex == txt.Length) {
                        if (!tb.MoveFocus(new TraversalRequest(FocusNavigationDirection.Right)))
                           tb.MoveFocus(new TraversalRequest(FocusNavigationDirection.Next));
                        e.Handled = true;
                     }
                  }
                  break;
               case Key.Up:
                  ((TextBox)sender).MoveFocus(new TraversalRequest(FocusNavigationDirection.Up));
                  e.Handled = true;
                  break;
               case Key.Down:
                  ((TextBox)sender).MoveFocus(new TraversalRequest(FocusNavigationDirection.Down));
                  e.Handled = true;
                  break;
            }
         }
      }

      public static bool GetEnableArrowKeyTraverse(DependencyObject d) => (bool)d.GetValue(EnableArrowKeyTraverseProperty);
      public static void SetEnableArrowKeyTraverse(DependencyObject d, bool value) => d.SetValue(EnableArrowKeyTraverseProperty, value);
      #endregion

      #region InputFilterRegex
      public static readonly DependencyProperty InputFilterProperty = DependencyProperty.RegisterAttached(
         "InputFilter", typeof(TextBoxInputFilter), typeof(TextBoxBehavior), new PropertyMetadata(OnInputFilterChanged));

      static void OnInputFilterChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) {
         var text_box = ExceptionAssert.Argument.Is<TextBox>(d, nameof(d));
         if (e.OldValue is TextBoxInputFilter old_filter)
            old_filter.detach(text_box);
         if (e.NewValue is TextBoxInputFilter new_filter)
            new_filter.attach(text_box);
      }

      public static TextBoxInputFilter GetInputFilter(DependencyObject d) => d.GetValue(InputFilterProperty) as TextBoxInputFilter;
      public static void SetInputFilter(DependencyObject d, TextBoxInputFilter value) => d.SetValue(InputFilterProperty, value);
      #endregion

      #region IncrementDecrementOnMouseClick
      public static readonly DependencyProperty IncrementDecrementOnMouseClickProperty = DependencyProperty.RegisterAttached(
         "IncrementDecrementOnMouseClick", typeof(bool), typeof(TextBoxBehavior), new PropertyMetadata(false, IncrementDecrementOnMouseClickPropertyChanged)
         );

      public static bool GetIncrementDecrementOnMouseClick(TextBox element) { return (bool)element.GetValue(IncrementDecrementOnMouseClickProperty); }
      public static void SetIncrementDecrementOnMouseClick(TextBox element, bool value) { element.SetValue(IncrementDecrementOnMouseClickProperty, value); }


      private static void IncrementDecrementOnMouseClickPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) {
         var text_box = d as TextBox;
         if (text_box != null) {
            if ((bool)e.NewValue) {
               text_box.PreviewMouseLeftButtonDown += OnMouseLeftButtonDownDecrementValue;
               text_box.PreviewMouseRightButtonDown += OnMouseRightButtonDownIncrementValue;
            }
            else {
               text_box.PreviewMouseLeftButtonDown -= OnMouseLeftButtonDownDecrementValue;
               text_box.PreviewMouseRightButtonDown -= OnMouseRightButtonDownIncrementValue;
            }
         }
      }

      private static void OnMouseRightButtonDownIncrementValue(object sender, MouseButtonEventArgs e) {
         var text_box = sender as TextBox;
         if (text_box == null || String.IsNullOrWhiteSpace(text_box.Text)) {
            return;
         }
         else {
            if (uint.TryParse(text_box.Text, out uint val)) {
               if (val < uint.MaxValue) {
                  val++;
                  text_box.Text = val.ToString();
               }
            }
         }
      }

      private static void OnMouseLeftButtonDownDecrementValue(object sender, MouseButtonEventArgs e) {
         var text_box = sender as TextBox;
         if (text_box == null || String.IsNullOrWhiteSpace(text_box.Text)) {
            return;
         }
         else {
            if (uint.TryParse(text_box.Text, out uint val)) {
               if (val > uint.MinValue) {
                  val--;
                  text_box.Text = val.ToString();
               }
            }
         }
      }
      #endregion

   }
}