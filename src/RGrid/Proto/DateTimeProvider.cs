using EqualityComparer.Extensions.Utilities;
using ProtoBuf;
using System;
using System.ComponentModel;

namespace RGrid.Proto {
   public enum DateTimeProviderMode {
      [Description("(Explicit)")]
      Explicit,
      Today
   }

   [ProtoContract]
   public struct DateTimeProvider : IEquatable<DateTimeProvider> {
      [ProtoMember(1)]
      readonly DateTimeProviderMode _mode;
      [ProtoMember(2)]
      readonly DateTime? _explicit_date;

      public DateTimeProvider(DateTimeProviderMode mode, DateTime? explicit_date = null) {
         _mode = mode;
         _explicit_date = explicit_date;
      }

      public DateTimeProviderMode mode => _mode;
      public DateTime? explicit_date => _explicit_date;

      public DateTimeProvider with(DateTimeProviderMode mode) =>
         new DateTimeProvider(mode, _explicit_date);

      public DateTimeProvider with(DateTime? explicit_date) =>
         new DateTimeProvider(_mode, explicit_date);

      public bool Equals(DateTimeProvider other) =>
         _mode == other._mode &&
         EqualityUtils.NullableEquals(_explicit_date, other._explicit_date);

      public override bool Equals(object obj) =>
         obj is DateTimeProvider dtp && Equals(dtp);

      public override int GetHashCode() =>
         _explicit_date.HasValue ? HashUtils.Phase((int)_mode, _explicit_date.Value.GetHashCode()) : (int)_mode;

      public static bool operator ==(DateTimeProvider a, DateTimeProvider b) => a.Equals(b);
      public static bool operator !=(DateTimeProvider a, DateTimeProvider b) => !a.Equals(b);
   }

   static class DateTimeProviderExtensions {
      public static DateTime get_date_time(this DateTimeProvider dtp) {
         switch (dtp.mode) {
            case DateTimeProviderMode.Today: return DateTime.Today;
            case DateTimeProviderMode.Explicit:
            default: return dtp.explicit_date.GetValueOrDefault();
         }
      }
   }
}
