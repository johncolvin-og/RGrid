using EqualityComparer.Extensions.Utilities;
using ProtoBuf;
using System;
using System.Windows;

namespace mbts.ui.settings {  
   [ProtoContract]
   struct NullableFontWeightProto : IEquatable<NullableFontWeightProto> {
      [ProtoMember(1)]
      readonly ushort? _value;

      public NullableFontWeightProto(FontWeight? value) =>
         _value = (ushort?)value?.ToOpenTypeWeight();

      public NullableFontWeightProto(ushort value) =>
         _value = value;

      public FontWeight? value {
         get {
            if (_value.HasValue)
               return FontWeight.FromOpenTypeWeight(_value.Value);
            return null;
         }
      }

      public bool Equals(NullableFontWeightProto other) =>
         EqualityUtils.NullableEquals(_value, other._value);

      public override bool Equals(object obj) =>
         obj is NullableFontWeightProto ncp && Equals(ncp);

      public override int GetHashCode() =>
         _value.HasValue ? _value.Value : 0;

      public static bool operator ==(NullableFontWeightProto a, NullableFontWeightProto b) => a.Equals(b);
      public static bool operator !=(NullableFontWeightProto a, NullableFontWeightProto b) => !a.Equals(b);
   }
}
