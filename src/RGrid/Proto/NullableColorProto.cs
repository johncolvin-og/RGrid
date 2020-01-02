using EqualityComparer.Extensions.Utilities;
using ProtoBuf;
using RGrid.WPF;
using System;
using System.Windows.Media;

namespace RGrid.Proto {
   [ProtoContract]
   struct NullableColorProto : IEquatable<NullableColorProto> {
      [ProtoMember(1)]
      readonly uint? _value;

      public NullableColorProto(Color? value) =>
         _value = value?.rgb_color();

      public NullableColorProto(uint value) =>
         _value = value;

      public Color? value {
         get {
            if (_value.HasValue)
               return _value.Value.rgb_color();
            return null;
         }
      }

      public bool Equals(NullableColorProto other) =>
         EqualityUtils.NullableEquals(_value, other._value);

      public override bool Equals(object obj) =>
         obj is NullableColorProto ncp && Equals(ncp);

      public override int GetHashCode() =>
         _value.HasValue ? (int)_value.Value : 0;

      public static bool operator ==(NullableColorProto a, NullableColorProto b) => a.Equals(b);
      public static bool operator !=(NullableColorProto a, NullableColorProto b) => !a.Equals(b);
   }
}