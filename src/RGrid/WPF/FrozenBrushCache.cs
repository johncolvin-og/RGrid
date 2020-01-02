using RGrid.Utility;
using System;
using System.Collections.Generic;
using System.Windows.Media;

namespace RGrid.WPF {
   readonly struct BrushKey : IEquatable<BrushKey> {
      readonly int _color;
      // Opacity is converted to a ushort, so the 32 hash bits can be split in half: 16 for color, and 16 for opacity.
      // This costs some precision wrt opacity; it is doubtful the human eye would be able to discern the difference in any case.
      readonly ushort _opacity;

      BrushKey(int color, ushort opacity) {
         _color = color;
         _opacity = opacity;
      }

      public static BrushKey get(Color color, double opacity = 1.0) {
         ExceptionAssert.Argument.Between(opacity, 0.0, 1.0, nameof(opacity));
         unchecked {
            var opac = (ushort)(opacity * ushort.MaxValue);
            // color has a perfect hash (it is 32 bits, so it damn well better).
            return new BrushKey(color.GetHashCode(), opac);
         }
      }

      public bool Equals(BrushKey other) =>
         _color == other._color &&
         _opacity == other._opacity;

      public override bool Equals(object obj) =>
         obj is BrushKey other && Equals(other);

      public override int GetHashCode() =>
         // ushort.MaxValue indicates opacity of '1.'
         // The value we have is the actual opacity multiplied by 100,000;
         // so 1 would actually be a very small opacity: 0.0001.
         _opacity == ushort.MaxValue ?
            // the last bit indicates if opacity is zero
            _color | -1 :
            ((ushort)_color | (_opacity << 16) | -1) ^ -1;
   }

   static class FrozenBrushCache {
      [ThreadStatic]
      static Dictionary<BrushKey, SolidColorBrush> _brushes;

      public static SolidColorBrush get_brush(Color color) =>
         get_brush(color, 1.0);

      public static SolidColorBrush get_brush(Color color, double opacity) {
         if (_brushes == null)
            _brushes = new Dictionary<BrushKey, SolidColorBrush>();
         var key = BrushKey.get(color, opacity);
         if (!_brushes.TryGetValue(key, out var brush)) {
            brush = new SolidColorBrush(color) { Opacity = opacity };
            brush.Freeze();
            _brushes[key] = brush;
         }
         return brush;
      }
   }
}