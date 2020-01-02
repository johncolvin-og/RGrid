using ProtoBuf;
using System;

namespace RGrid.Filters {
   public class DoubleMinMax<TRow> : ComparableMinMax<TRow, double, DoubleMinMaxState> {
      public DoubleMinMax(Func<TRow, double> get_row_val, string prop_name)
         : base(get_row_val, prop_name) { }
   }

   [ProtoContract]
   public class DoubleStatus : INullableStructProto<double> {
      [ProtoMember(1)]
      bool _active;
      [ProtoMember(2)]
      double _value;

      public bool active => _active;
      public double? value {
         get => _active ? new double?(_value) : new double?();
         set { if (_active = value.HasValue && !double.IsNaN(value.Value)) _value = value.Value; }
      }
   }

   [ProtoContract]
   public class DoubleMinMaxState : IMinMaxState<double> {
      [ProtoMember(1)]
      readonly DoubleStatus _minimum;
      [ProtoMember(2)]
      readonly DoubleStatus _maximum;

      public DoubleMinMaxState() {
         _minimum = new DoubleStatus();
         _maximum = new DoubleStatus();
      }

      public INullableStructProto<double> minimum => _minimum;
      public INullableStructProto<double> maximum => _maximum;
   }
}