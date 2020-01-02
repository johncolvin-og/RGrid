using System;
using System.Windows.Input;

namespace RGrid.Utility {
   static class MouseHelper {
      public static MouseButtonState button_state(MouseButton button) {
         switch (button) {
            case MouseButton.Left: return Mouse.LeftButton;
            case MouseButton.Middle: return Mouse.MiddleButton;
            case MouseButton.Right: return Mouse.RightButton;
            case MouseButton.XButton1: return Mouse.XButton1;
            case MouseButton.XButton2: return Mouse.XButton2;
            default: throw new ArgumentException($"Unexpected value '{button}.'", nameof(button));
         }
      }
   }
}
