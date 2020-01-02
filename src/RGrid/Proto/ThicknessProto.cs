using EqualityComparer.Extensions.Utilities;
using ProtoBuf;
using System;
using System.Windows;

namespace RGrid.Proto {
   [ProtoContract]
   public struct ThicknessProto : IEquatable<ThicknessProto> {
      const float _default_side = 1f;

      [ProtoMember(1)]
      readonly float _left;
      [ProtoMember(2)]
      readonly float _top;
      [ProtoMember(3)]
      readonly float _right;
      [ProtoMember(4)]
      readonly float _bottom;

      public ThicknessProto(Thickness thickness) {
         _left = (float)thickness.Left;
         _top = (float)thickness.Top;
         _right = (float)thickness.Right;
         _bottom = (float)thickness.Bottom;
      }

      public Thickness to_thickness() =>
         new Thickness(_left, _top, _right, _bottom);

      public bool Equals(ThicknessProto other) =>
         _left == other._left &&
         _top == other._top &&
         _right == other._right &&
         _bottom == other._bottom;

      public override bool Equals(object obj) =>
         obj is ThicknessProto tp && Equals(tp);

      public override int GetHashCode() =>
         HashUtils.Phase((int)_left, (int)_top, (int)_right, (int)_bottom);

      public static bool operator ==(ThicknessProto a, ThicknessProto b) => a.Equals(b);
      public static bool operator !=(ThicknessProto a, ThicknessProto b) => !a.Equals(b);
   }
}