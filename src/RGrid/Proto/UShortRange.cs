using EqualityComparer.Extensions.Utilities;
using ProtoBuf;
using System;

namespace RGrid.Proto {
   [ProtoContract]
   public struct UShortRange : IEquatable<UShortRange> {
      [ProtoMember(1)]
      readonly ushort _minimum;
      [ProtoMember(2)]
      readonly ushort _maximum;

      public UShortRange(ushort minimum, ushort maximum) {
         _minimum = minimum;
         _maximum = maximum;
      }

      public ushort minimum => _minimum;
      public ushort maximum => _maximum;

      public UShortRange with(ushort? minimum = null, ushort? maximum = null) =>
         new UShortRange(minimum ?? _minimum, maximum ?? _maximum);

      public bool Equals(UShortRange other) =>
         other._minimum == _minimum && other.maximum == _maximum;

      public override bool Equals(object obj) =>
         obj is UShortRange ur && Equals(ur);

      public override int GetHashCode() =>
         HashUtils.Phase((int)_minimum, (int)_maximum);

      public override string ToString() => $"{_minimum} to {_maximum}";

      public static bool operator ==(UShortRange a, UShortRange b) => a.Equals(b);
      public static bool operator !=(UShortRange a, UShortRange b) => !a.Equals(b);
   }
}