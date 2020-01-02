using EqualityComparer.Extensions.Utilities;
using ProtoBuf;
using RGrid.Utility;
using RGrid.WPF;
using System;
using System.Windows.Media;

namespace RGrid.Proto {
   [ProtoContract]
   public struct ColorOpacityProto : IEquatable<ColorOpacityProto> {
      [ProtoMember(1)]
      readonly uint _color;
      [ProtoMember(2)]
      readonly double _opacity;

      public ColorOpacityProto(Color color = default(Color), double opacity = 1.0)
         : this(color.rgb_color(), opacity) { }

      public ColorOpacityProto(uint color = 0u, double opacity = 1.0) {
         _color = color;
         _opacity = opacity;
      }

      public Color color => _color.rgb_color();
      public double opacity => _opacity;

      public ColorOpacityProto with(uint? color = null, double? opacity = null) =>
         new ColorOpacityProto(color ?? _color, opacity ?? _opacity);

      public SolidColorBrush to_brush() =>
         new SolidColorBrush(_color.rgb_color()) { Opacity = _opacity };

      public static ColorOpacityProto from_brush(Brush brush) {
         var scb = ExceptionAssert.Argument.Is<SolidColorBrush>(brush, nameof(brush));
         return from_brush(scb);
      }

      public static ColorOpacityProto from_brush(SolidColorBrush brush) =>
         new ColorOpacityProto(brush.Color, brush.Opacity);

      public bool Equals(ColorOpacityProto other) =>
         _color == other._color &&
         _opacity == other._opacity;

      public override bool Equals(object obj) =>
         obj is ColorOpacityProto cop && Equals(cop);

      public override int GetHashCode() =>
         HashUtils.Phase((int)_color, _opacity.GetHashCode());

      public static bool operator ==(ColorOpacityProto a, ColorOpacityProto b) => a.Equals(b);
      public static bool operator !=(ColorOpacityProto a, ColorOpacityProto b) => !a.Equals(b);
   }
}