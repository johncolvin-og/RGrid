using ProtoBuf;
using System;

namespace RGrid.Filters {
   public class UIntMinMax<TRow> : ComparableMinMax<TRow, uint, UIntMinMaxState> {
      public UIntMinMax(Func<TRow, uint> get_row_val, string prop_name)
         : base(get_row_val, prop_name) { }
   }

   [ProtoContract]
   public class UIntStatus : INullableStructProto<uint> {
      [ProtoMember(1)]
      bool _active;
      [ProtoMember(2)]
      uint _value;

      public bool active => _active;
      public uint? value {
         get => _active ? new uint?(_value) : new uint?();
         set { if (_active = value.HasValue) _value = value.Value; }
      }
   }

   [ProtoContract]
   public class UIntMinMaxState : IMinMaxState<uint> {
      [ProtoMember(1)]
      readonly UIntStatus _minimum;
      [ProtoMember(2)]
      readonly UIntStatus _maximum;

      public UIntMinMaxState() {
         _minimum = new UIntStatus();
         _maximum = new UIntStatus();
      }

      public INullableStructProto<uint> minimum => _minimum;
      public INullableStructProto<uint> maximum => _maximum;
   }
}