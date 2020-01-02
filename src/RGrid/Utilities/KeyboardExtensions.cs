using System.Windows.Input;

namespace RGrid.Utility {
   public static class KeyboardExtensions {
      public static bool IsCtrlDown() { return Key.LeftCtrl.IsDown() || Key.RightCtrl.IsDown(); }
      public static bool IsShiftDown() { return Key.LeftShift.IsDown() || Key.RightShift.IsDown(); }

      public static bool IsDown(this Key key) { return Keyboard.IsKeyDown(key); }
   }
}