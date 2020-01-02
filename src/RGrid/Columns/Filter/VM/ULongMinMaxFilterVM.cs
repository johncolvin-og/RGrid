using ProtoBuf;
using System;

namespace RGrid.Filters {
   class ULongMinMax<TRow> : ComparableMinMax<TRow, ulong, ULongMinMaxState> {
      public ULongMinMax(Func<TRow, ulong> get_row_val, string prop_name)
         : base(get_row_val, prop_name) { }
   }

   [ProtoContract]
   class ULongStatus : INullableStructProto<ulong> {
      [ProtoMember(1)]
      bool _active;
      [ProtoMember(2)]
      ulong _value;

      public bool active => _active;
      public ulong? value {
         get => _active ? new ulong?(_value) : new ulong?();
         set { if (_active = value.HasValue) _value = value.Value; }
      }
   }

   [ProtoContract]
   class ULongMinMaxState : IMinMaxState<ulong> {
      [ProtoMember(1)]
      readonly ULongStatus _minimum;
      [ProtoMember(2)]
      readonly ULongStatus _maximum;

      public ULongMinMaxState() {
         _minimum = new ULongStatus();
         _maximum = new ULongStatus();
      }

      public INullableStructProto<ulong> minimum => _minimum;
      public INullableStructProto<ulong> maximum => _maximum;
   }
}