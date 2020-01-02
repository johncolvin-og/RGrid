using ProtoBuf;
using System;

namespace RGrid.Filters {
   class IntMinMax<TRow> : ComparableMinMax<TRow, int, IntMinMaxState> {
      public IntMinMax(Func<TRow, int> get_row_val, string prop_name)
         : base(get_row_val, prop_name) { }
   }

   [ProtoContract]
   class IntStatus : INullableStructProto<int> {
      [ProtoMember(1)]
      bool _active;
      [ProtoMember(2)]
      int _value;

      public bool active => _active;
      public int? value {
         get => _active ? new int?(_value) : new int?();
         set { if (_active = value.HasValue) _value = value.Value; }
      }
   }

   [ProtoContract]
   class IntMinMaxState : IMinMaxState<int> {
      [ProtoMember(1)]
      readonly IntStatus _minimum;
      [ProtoMember(2)]
      readonly IntStatus _maximum;

      public IntMinMaxState() {
         _minimum = new IntStatus();
         _maximum = new IntStatus();
      }

      public INullableStructProto<int> minimum => _minimum;
      public INullableStructProto<int> maximum => _maximum;
   }
}