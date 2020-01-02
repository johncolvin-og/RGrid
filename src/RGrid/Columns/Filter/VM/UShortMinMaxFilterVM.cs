using ProtoBuf;
using System;

namespace RGrid.Filters {
   class UShortMinMax<TRow> : ComparableMinMax<TRow, ushort, UShortMinMaxState> {
      public UShortMinMax(Func<TRow, ushort> get_row_val, string prop_name)
         : base(get_row_val, prop_name) { }
   }

   [ProtoContract]
   class UShortStatus : INullableStructProto<ushort> {
      [ProtoMember(1)]
      bool _active;
      [ProtoMember(2)]
      ushort _value;

      public bool active => _active;
      public ushort? value {
         get => _active ? new ushort?(_value) : new ushort?();
         set { if (_active = value.HasValue) _value = value.Value; }
      }
   }

   [ProtoContract]
   class UShortMinMaxState : IMinMaxState<ushort> {
      [ProtoMember(1)]
      readonly UShortStatus _minimum;
      [ProtoMember(2)]
      readonly UShortStatus _maximum;

      public UShortMinMaxState() {
         _minimum = new UShortStatus();
         _maximum = new UShortStatus();
      }

      public INullableStructProto<ushort> minimum => _minimum;
      public INullableStructProto<ushort> maximum => _maximum;
   }
}