using Disposable.Extensions;
using Disposable.Extensions.Utilities;
using RGrid.WPF;
using System;
using System.Windows.Input;

namespace RGrid.Filters {
   public abstract class FilterVMBase<TRow, TValue, TState> : ViewModelBaseWithValidation, IDataGridColumnPersistentFilter<TRow, TState>, IRequestBindingSourceUpdate {
      protected readonly Func<TRow, TValue> _get_row_val;

      readonly DelegateProperty<bool?> _active;
      bool _open;
      IObjSnapshot _snapshot;

      protected FilterVMBase(Func<TRow, TValue> get_row_val, string prop_name) {
         _get_row_val = get_row_val;
         this.prop_name = prop_name;
         _active = backing(nameof(active), () => HasErrors ? new bool?() : _get_active());
         _snapshot = _get_snapshot();
         Apply = new DelegateCommand(_apply);
         Cancel = new DelegateCommand(_cancel);
         Clear = new DelegateCommand(_clear);
      }

      public event Action filter_changed;
      public event Action RequestUpdate;

      public string prop_name { get; }
      public bool? active => _active.Value;
      public bool open {
         get => _open;
         set {
            _open = value;
            if (_open)
               _snapshot = _get_snapshot();
            else if (_snapshot != null) {
               RequestUpdate?.Invoke();
               _snapshot.revert = true;
               DisposableUtils.Dispose(ref _snapshot);
            }
         }
      }
      public bool Filter(TRow row) => _filter(_get_row_val(row));
      public ICommand Apply { get; }
      public ICommand Cancel { get; }
      public ICommand Clear { get; }

      public abstract TState GetState();

      public void LoadState(TState state) {
         _load_state_internal(state);
         _snapshot = _get_snapshot();
         _validate();
         _raise_filter_changed();
      }

      protected abstract bool _get_active();
      protected abstract bool _filter(TValue value);
      protected abstract void _load_state_internal(TState state);

      protected virtual void _apply() {
         RequestUpdate?.Invoke();
         _snapshot = null;
         _validate();
         if (!HasErrors) {
            open = false;
            RaisePropertyChanged(nameof(open));
         }
         _raise_filter_changed();
      }

      protected virtual void _cancel() {
         if (_snapshot != null)
            _snapshot.revert = true;
         DisposableUtils.Dispose(ref _snapshot);
         _snapshot = _get_snapshot();
         open = false;
         RaisePropertyChanged(nameof(open));
      }

      protected virtual void _clear() { }

      protected IDisposable _clear_properties(bool close=true) {
         _snapshot = null;
         var dpc = _get_snapshot();
         return DisposableFactory.Create(() => {
            dpc.Dispose();
            _validate();
            _raise_filter_changed();
            _snapshot = _get_snapshot();
         });
      }

      protected void _close() {
         open = false;
         RaisePropertyChanged(nameof(open));
      }

      protected void _destroy_snapshot() =>
         _snapshot = null;

      protected virtual IObjSnapshot _get_snapshot() =>
         detect_property_changes_except(nameof(open), nameof(active));

      protected void _raise_filter_changed() {
         _active.refresh();
         filter_changed?.Invoke();
      }

      protected virtual void _validate() { }

      bool IDataGridColumnFilter.filter(object row) =>
         Filter(ConvertUtils.try_convert<TRow>(row));
   }
}