using EqualityComparer.Extensions.Utilities;
using Monitor.Render.Utilities;
using RGrid.Utility;
using System;
using System.Collections.Generic;
using System.Windows.Media;

namespace RGrid.WPF {
   readonly struct PenKey : IEquatable<PenKey> {
      // This code asserts that thickness will not exceed 100
      // this is an arbitrary max that can adjusted if needed
      const ushort
         _max_thickness = 100,
         _thickness_mult = ushort.MaxValue / _max_thickness;

      readonly BrushKey _brush_key;
      readonly ushort _thickness;

      PenKey(BrushKey brush_key, ushort thickness) {
         _brush_key = brush_key;
         _thickness = thickness;
      }

      public static PenKey get(BrushKey brush_key, double thickness) {
         ExceptionAssert.Argument.Between(thickness, 0, _max_thickness, nameof(thickness));
         return new PenKey(brush_key, (ushort)(thickness * _thickness_mult));
      }

      public bool Equals(PenKey other) =>
         _brush_key.Equals(other._brush_key) &&
         _thickness == other._thickness;

      public override bool Equals(object obj) =>
         obj is BrushKey other && Equals(other);

      public override int GetHashCode() =>
         HashUtils.Phase(
            _brush_key.GetHashCode(),
            _thickness.GetHashCode());
   }

   static class FrozenPenCache {
      [ThreadStatic]
      static Dictionary<PenKey, Pen> _pens;

      public static Pen get_pen(Color color) =>
         get_pen(color, DPIUtils.pixel_unit, 1);

      public static Pen get_pen(Color color, double thickness, double opacity) {
         if (_pens == null)
            _pens = new Dictionary<PenKey, Pen>();
         var key = PenKey.get(BrushKey.get(color, opacity), thickness);
         if (!_pens.TryGetValue(key, out var pen)) {
            _pens[key] = pen = new Pen(FrozenBrushCache.get_brush(color, opacity), thickness).frozen();
         }
         return pen;
      }
   }
}
