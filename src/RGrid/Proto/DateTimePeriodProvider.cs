using EqualityComparer.Extensions.Utilities;
using ProtoBuf;
using System;

namespace RGrid.Proto {
   [ProtoContract]
   public struct DateTimePeriodProvider : IEquatable<DateTimePeriodProvider> {
      [ProtoMember(1)]
      readonly DateTimeProvider _begin;
      [ProtoMember(2)]
      readonly DateTimeProvider _end;

      public DateTimePeriodProvider(DateTimeProvider begin, DateTimeProvider end) {
         _begin = begin;
         _end = end;
      }

      public DateTimeProvider begin => _begin;
      public DateTimeProvider end => _end;

      public DateTimePeriodProvider with(DateTimeProvider? begin = null, DateTimeProvider? end = null) =>
         new DateTimePeriodProvider(begin ?? _begin, end ?? _end);

      public bool Equals(DateTimePeriodProvider other) =>
         _begin.Equals(other._begin) &&
         _end.Equals(other._end);

      public override bool Equals(object obj) =>
         obj is DateTimePeriodProvider dtpp && Equals(dtpp);

      public override int GetHashCode() =>
         HashUtils.Phase(_begin.GetHashCode(), _end.GetHashCode());

      public static bool operator ==(DateTimePeriodProvider a, DateTimePeriodProvider b) => a.Equals(b);
      public static bool operator !=(DateTimePeriodProvider a, DateTimePeriodProvider b) => !a.Equals(b);
   }
}
