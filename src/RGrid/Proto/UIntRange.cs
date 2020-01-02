using EqualityComparer.Extensions.Utilities;
using ProtoBuf;
using System;

namespace RGrid.Proto {
   [ProtoContract]
   public struct UIntRange : IEquatable<UIntRange> {
      [ProtoMember(1)]
      readonly uint _minimum;
      [ProtoMember(2)]
      readonly uint _maximum;

      public UIntRange(uint minimum, uint maximum) {
         _minimum = minimum;
         _maximum = maximum;
      }

      public uint minimum => _minimum;
      public uint maximum => _maximum;

      public UIntRange with(uint? minimum = null, uint? maximum = null) =>
         new UIntRange(minimum ?? _minimum, maximum ?? _maximum);

      public bool Equals(UIntRange other) =>
         other._minimum == _minimum && other.maximum == _maximum;

      public override bool Equals(object obj) =>
         obj is UIntRange ur && Equals(ur);

      public override int GetHashCode() =>
         HashUtils.Phase((int)_minimum, (int)_maximum);

      public override string ToString() => $"{_minimum} to {_maximum}";

      public static bool operator ==(UIntRange a, UIntRange b) => a.Equals(b);
      public static bool operator !=(UIntRange a, UIntRange b) => !a.Equals(b);
   }
}