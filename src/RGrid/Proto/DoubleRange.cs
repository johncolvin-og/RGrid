using EqualityComparer.Extensions.Utilities;
using ProtoBuf;
using System;

namespace RGrid.Proto {
   [ProtoContract]
   public struct DoubleRange : IEquatable<DoubleRange> {
      [ProtoMember(1)]
      readonly double _minimum;
      [ProtoMember(2)]
      readonly double _maximum;

      public DoubleRange(double minimum, double maximum) {
         _minimum = minimum;
         _maximum = maximum;
      }

      public double minimum => _minimum;
      public double maximum => _maximum;

      public DoubleRange with(double? minimum = null, double? maximum = null) =>
         new DoubleRange(minimum ?? _minimum, maximum ?? _maximum);

      public bool Equals(DoubleRange other) =>
         other._minimum == _minimum && other.maximum == _maximum;

      public override bool Equals(object obj) =>
         obj is DoubleRange dr && Equals(dr);

      public override int GetHashCode() =>
         HashUtils.Phase(_minimum.GetHashCode(), _maximum.GetHashCode());

      public override string ToString() => $"{_minimum} to {_maximum}";

      public static bool operator ==(DoubleRange a, DoubleRange b) => a.Equals(b);
      public static bool operator !=(DoubleRange a, DoubleRange b) => !a.Equals(b);
   }
}