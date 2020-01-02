using RGrid.Controls;
using ProtoBuf;
using System;
using System.Globalization;

namespace RGrid {
   [ProtoContract]
   public struct TimeFilterTuple : IEquatable<TimeFilterTuple> {
      [ProtoMember(1)]
      readonly TimeFilterMode _mode;
      [ProtoMember(2)]
      readonly TimeTextTuple _begin;
      [ProtoMember(3)]
      readonly TimeTextTuple _end;

      public TimeFilterTuple(TimeFilterMode mode, TimeTextTuple begin, TimeTextTuple end) {
         _mode = mode;
         _begin = begin;
         _end = end;
      }

      public TimeFilterMode mode => _mode;
      public TimeTextTuple begin => _begin;
      public TimeTextTuple end => _end;
      public bool is_active =>
         _mode == TimeFilterMode.Today ||
         (_mode == TimeFilterMode.Period && (_begin.result.HasValue || _end.result.HasValue));


      public bool Equals(TimeFilterTuple other) =>
         other._mode == _mode && other._begin.Equals(_begin) && other._end.Equals(_end);

      public override bool Equals(object obj) =>
         obj is TimeFilterTuple && Equals((TimeFilterTuple)obj);

      public override int GetHashCode() =>
         _mode.GetHashCode() ^ _begin.GetHashCode() ^ _end.GetHashCode();

      public override string ToString() =>
         $"Mode: {_mode}\nBegin: {_begin}\nEnd:{_end}";
   }

   [ProtoContract]
   public struct TimeTextTuple : IEquatable<TimeTextTuple> {
      [ProtoMember(1)]
      readonly string _month_text;
      [ProtoMember(2)]
      readonly string _day_text;
      [ProtoMember(3)]
      readonly string _year_text;
      [ProtoMember(4)]
      readonly string _time_text;

      public static TimeTextTuple FromTicks(long ticks) =>
         FromDateTime(new DateTime(ticks));

      public static TimeTextTuple FromDateTime(DateTime date_time) => FromDateTime(date_time, "t");
      public static TimeTextTuple FromDateTime(DateTime date_time, string time_format) =>
         new TimeTextTuple(DateTimeFormatInfo.CurrentInfo.AbbreviatedMonthNames[date_time.Month - 1], date_time.Day.ToString(), date_time.Year.ToString(), date_time.ToString(time_format));

      public TimeTextTuple(string month_text, string day_text, string year_text, string time_text) {
         _month_text = month_text;
         _day_text = day_text;
         _year_text = year_text;
         _time_text = time_text;
      }

      public string month_text => _month_text;
      public string day_text => _day_text;
      public string year_text => _year_text;
      public string time_text => _time_text;
      public DateTime? result => _parse(_month_text, _day_text, _year_text, _time_text);
      public bool is_valid =>
         result.HasValue || (
            string.IsNullOrEmpty(_month_text) &&
            string.IsNullOrEmpty(_day_text) &&
            string.IsNullOrEmpty(_year_text) &&
            string.IsNullOrEmpty(_time_text)
         );

      public static TimeTextTuple TodayEmptyTime {
         get {
            var td = DateTime.Today;
            return new TimeTextTuple(td.Month.ToString(), td.Day.ToString(), td.Year.ToString(), string.Empty);
         }
      }

      public bool Equals(TimeTextTuple other) =>
         other._month_text == month_text &&
         other._day_text == _day_text &&
         other._year_text == year_text &&
         other._time_text == _time_text;

      public override bool Equals(object obj) =>
         obj is TimeTextTuple && Equals((TimeTextTuple)obj);

      public override int GetHashCode() =>
         (_month_text ?? string.Empty).GetHashCode() ^
         (_day_text ?? string.Empty).GetHashCode() ^
         (_year_text ?? string.Empty).GetHashCode() ^
         (_time_text ?? string.Empty).GetHashCode();

      public override string ToString() =>
         $"{_month_text ?? string.Empty}/" +
         $"{_day_text ?? string.Empty}/" +
         $"{_year_text ?? string.Empty} " +
         $"{_time_text ?? string.Empty}";


      static DateTime? _parse(string month_text, string day_text, string year_text, string time_text) {
         TimeSpan? t = _parse_time(time_text);
         if (t.HasValue) {
            DateTime? d = _parse($"{month_text}/{day_text}/{year_text}");
            if (d.HasValue) return d.Value.Date.Add(t.Value);
         }
         return null;
      }

      static TimeSpan? _parse_time(string text) {
         DateTime? dt = _parse(text);
         if (dt.HasValue) {
            return dt.Value.TimeOfDay;
         } else if (TimeSpan.TryParse(text, out TimeSpan result) && result < TimeSpan.FromDays(1)) {
            return result;
         } else return null;
      }

      static DateTime? _parse(string text) {
         if (!string.IsNullOrEmpty(text) && DateTime.TryParse(text, out DateTime result))
            return result;
         return null;
      }
   }
}