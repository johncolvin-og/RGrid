using RGrid.Utility;
using System;
using System.Text.RegularExpressions;
using System.Windows.Controls;
using System.Windows.Input;

namespace RGrid.WPF {
   abstract class TextBoxInputFilter : IAttachable<TextBox> {
      public static TextBoxInputFilter create(Func<string, bool> allow) =>
         new AnonymousFilter(allow);

      public static TextBoxInputFilter create(Regex regex, bool allow_null = true) =>
         allow_null ?
            new AnonymousFilter(txt => txt == null || regex.IsMatch(txt)) :
            new AnonymousFilter(regex.IsMatch);

      public void attach(TextBox target) =>
         target.PreviewTextInput += _on_PreviewTextInput;

      public void detach(TextBox target) =>
         target.PreviewTextInput -= _on_PreviewTextInput;

      protected abstract bool allow(string text);

      void _on_PreviewTextInput(object sender, TextCompositionEventArgs e) {
         var text_box = ExceptionAssert.Argument.Is<TextBox>(sender, nameof(sender));
         e.Handled = !allow(text_box.PeekText(e));
      }

      private class AnonymousFilter : TextBoxInputFilter {
         readonly Func<string, bool> _allow;

         public AnonymousFilter(Func<string, bool> allow) =>
            _allow = allow;

         protected override bool allow(string text) =>
            _allow(text);
      }
   } 
}