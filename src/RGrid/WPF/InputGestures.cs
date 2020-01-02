using System.Windows.Input;

namespace RGrid.WPF {
   class CustomInputBinding : InputBinding { }

   static class InputGestures {
      public static readonly InputGesture LeftTripleClick = new LeftTripleClickGesture();

      class LeftTripleClickGesture : InputGesture {
         public override bool Matches(object targetElement, InputEventArgs inputEventArgs) =>
            inputEventArgs is MouseButtonEventArgs mouse_args &&
            mouse_args.ChangedButton == MouseButton.Left &&
            mouse_args.ClickCount == 3;
      }
   }
}