using Disposable.Extensions;
using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;

namespace RGrid.WPF {
   static class TextElementHelper {
      public static IDisposable SubscribeKeyboardFocus(this TextBox text_box, Action<bool> callback) {
         text_box.GotKeyboardFocus += on_got_keyboard_focus;
         text_box.LostKeyboardFocus += on_lost_keyboard_focus;
         return DisposableFactory.Create(() => {
            text_box.GotKeyboardFocus -= on_got_keyboard_focus;
            text_box.LostKeyboardFocus -= on_lost_keyboard_focus;
         });
         void on_got_keyboard_focus(object sender, KeyboardFocusChangedEventArgs e) {
            if (e.NewFocus == text_box)
               callback(true);

         }
         void on_lost_keyboard_focus(object sender, KeyboardFocusChangedEventArgs e) {
            if (e.OldFocus == text_box)
               callback(false);
         }
      }

      public static void GetTargetProperty(DependencyObject d, out FrameworkElement text_element, out DependencyProperty text_property) {
         text_element = (FrameworkElement)d;
         if (text_element is TextBox) text_property = TextBox.TextProperty;
         else if (text_element is TextBlock) text_property = TextBlock.TextProperty;
         else throw new ArgumentException("Expected a TextBox or TextBlock.", "d");
      }

      public static BindingExpression GetTextBindingExpression(DependencyObject d) {
         FrameworkElement text_element;
         DependencyProperty text_property;
         GetTargetProperty(d, out text_element, out text_property);
         return text_element.GetBindingExpression(text_property);
      }

      public static DependencyProperty GetTextProperty(DependencyObject text_element) {
         if (text_element is TextBlock) return TextBlock.TextProperty;
         if (text_element is TextBox) return TextBox.TextProperty;
         throw new ArgumentException("Expected a TextBox or TextBlock.", "d");
      }

      public static bool try_get_binding_components<TConverter>(DependencyObject d, out BindingExpression be, out Binding binding, out TConverter converter)
         where TConverter : IValueConverter {
         be = GetTextBindingExpression(d);
         if (be != null) {
            binding = be.ParentBinding;
            var raw_converter = binding.Converter;
            if (!(raw_converter is TConverter))
               throw new InvalidOperationException($"Converter must be {typeof(TConverter).FullName}.");
            converter = (TConverter)raw_converter;
            return true;
         }
         converter = default(TConverter);
         binding = null;
         return false;
      }

      public static void UpdateTextBindingSource(DependencyObject text_element) {
         var be = GetTextBindingExpression(text_element);
         if (be != null) be.UpdateSource();
      }
   }
}