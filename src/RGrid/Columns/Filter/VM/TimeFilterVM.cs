using RGrid.Controls;
using System;
using System.ComponentModel;

namespace RGrid.Filters {
   interface ITimeFilterVM : INotifyDataErrorInfo {
      TimeFilterMode mode { get; }
      TimeTextTuple begin { get; }
      TimeTextTuple end { get; }
   }

   class TimeFilterVM<TRow> : FilterVMBase<TRow, DateTime, TimeFilterTuple>, ITimeFilterVM {
      TimeFilterMode _mode;
      TimeTextTuple _begin, _end;
      DateTime? _begin_dt, _end_dt;

      public TimeFilterVM(Func<TRow, DateTime> get_row_val, string prop_name) : base(get_row_val, prop_name) =>
         _set_default_values();

      public TimeFilterMode mode {
         get => _mode;
         set {
            _mode = value;
            if (HasErrors) _validate();
         }
      }

      public TimeTextTuple begin {
         get => _begin;
         set {
            _begin = value;
            _begin_dt = _begin.result;
            if (HasErrors) _validate();
         }
      }

      public TimeTextTuple end {
         get => _end;
         set {
            _end = value;
            _end_dt = _end.result;
            if (HasErrors) _validate();
         }
      }

      protected override void _clear() {
         _close();
         using (_clear_properties())
            _set_default_values();
      }

      protected override bool _filter(DateTime value) {
         switch (mode) {
            case TimeFilterMode.Today: return value.Date == DateTime.Today.Date;
            case TimeFilterMode.Period: return (!_begin_dt.HasValue || value >= _begin_dt.Value) && (!_end_dt.HasValue || value <= _end_dt.Value);
            default: return true;
         }
      }

      protected override bool _get_active() {
         switch (mode) {
            case TimeFilterMode.Today: return true;
            case TimeFilterMode.Period: return _begin_dt.HasValue || _end_dt.HasValue;
            default: return false;
         }
      }

      public override TimeFilterTuple GetState() =>
         new TimeFilterTuple(mode, _begin, _end);

      protected override void _load_state_internal(TimeFilterTuple state) {
         mode = state.mode;
         _begin = state.begin;
         _end = state.end;
         _begin_dt = _begin.result;
         _end_dt = _end.result;
         RaisePropertyChanged(nameof(mode), nameof(begin), nameof(end));
      }

      protected override void _validate() {
         using (validation(nameof(begin), nameof(end))) {
            if (mode == TimeFilterMode.Period) {
               bool begin_valid = _begin.is_valid, end_valid = _end.is_valid;
               validation_assert(begin_valid, "'From' is invalid.", nameof(begin));
               validation_assert(end_valid, "'To' is invalid.", nameof(end));
               if (begin_valid && end_valid) {
                  if (_begin_dt.HasValue) {
                     if (_end_dt.HasValue)
                        validation_assert(_end_dt.Value >= _begin_dt.Value, "'From' is after 'To.'", nameof(begin), nameof(end));
                  } else if (!_end_dt.HasValue) {
                     validation_assert(false, "Please set 'From' and/or 'To.'", nameof(begin));
                     validation_assert(false, string.Empty, nameof(end));
                  }
               }
            }
         }
      }

      void _set_default_values() {
         mode = TimeFilterMode.Anytime;
         _begin = TimeTextTuple.FromDateTime(DateTime.Today);
         _end = TimeTextTuple.FromDateTime(DateTime.Today.AddDays(1));
         _begin_dt = _begin.result;
         _end_dt = _end.result;
      }
   }
}