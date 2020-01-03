using Disposable.Extensions;
using Disposable.Extensions.Utilities;
using RGrid.WPF;
using System;
using System.Windows.Input;

namespace RGrid.Filters {
   public abstract class FilterVM<TRow, TValue, TState> : DisposableViewModelBase, IDataGridColumnPersistentFilter<TRow, TState>, IRequestBindingSourceUpdate {
      TState _snapshot;
      bool _is_open, _revert;
      bool? _is_active;
      IDisposable _predicate_sub;
      Func<TRow, bool> _predicate;

      public FilterVM() {
         Apply = new DelegateCommand(_apply);
         Clear = new DelegateCommand(_clear);
         Cancel = new DelegateCommand(_cancel);
      }

      public string prop_name { get; set; }

      public bool? IsActive {
         get => _is_active;
         set => Set(ref _is_active, value);
      }

      public bool IsOpen {
         get => _is_open;
         set {
            if (Set(ref _is_open, value)) {
               if (value) {
                  _snapshot = GetState();
                  _revert = true;
               } else if (_revert) {
                  LoadState(_snapshot);
                  _revert = false;
               }
            }
         }
      }

      public ICommand Apply { get; }
      public ICommand Clear { get; }
      public ICommand Cancel { get; }

      protected virtual TState DefaultState { get; }

      public event Action RequestUpdate;
      public event Action FilterChanged;

      public bool Filter(TRow row) => _predicate == null || _predicate(row);
      public bool Filter(object row) => Filter((TRow)row);
      public abstract TState GetState();
      public void LoadState(TState state) {
         _snapshot = state;
         _revert = _is_open;
         LoadStateCore(state);
      }

      protected abstract IObservable<Func<TRow, bool>> CreatePredicateSource(TState state);
      protected virtual void LoadStateCore(TState state) { }

      protected void OnFilterChanged() {
         if (HasErrors) {
            IsActive = null;
         } else {
            IsActive = _predicate != null;
         }
         FilterChanged?.Invoke();
      }

      void _apply() {
         _revert = false;
         DisposableUtils.Dispose(ref _predicate_sub);
         IsOpen = false;
         _predicate_sub = CreatePredicateSource(_snapshot)
            .Subscribe(pred => {
               _predicate = pred;
               OnFilterChanged();
            });
      }

      void _cancel() {
         if (_revert) {
            LoadState(_snapshot);
            _revert = false;
         }
      }

      void _clear() {
         _revert = false;
         LoadState(DefaultState);
      }
   }

   public abstract class FilterVMBase<TRow, TValue, TState> : ViewModelBaseWithValidation, IDataGridColumnPersistentFilter<TRow, TState>, IRequestBindingSourceUpdate {
      protected readonly Func<TRow, TValue> _get_row_val;

      readonly DelegateProperty<bool?> _active;
      bool _open;
      IObjSnapshot _snapshot;

      protected FilterVMBase(Func<TRow, TValue> get_row_val, string prop_name) {
         _get_row_val = get_row_val;
         this.prop_name = prop_name;
         _active = backing(nameof(IsActive), () => HasErrors ? new bool?() : _get_active());
         _snapshot = _get_snapshot();
         Apply = new DelegateCommand(_apply);
         Cancel = new DelegateCommand(_cancel);
         Clear = new DelegateCommand(_clear);
      }

      public event Action FilterChanged;
      public event Action RequestUpdate;

      public string prop_name { get; }
      public bool? IsActive => _active.Value;
      public bool IsOpen {
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
            IsOpen = false;
            RaisePropertyChanged(nameof(IsOpen));
         }
         _raise_filter_changed();
      }

      protected virtual void _cancel() {
         if (_snapshot != null)
            _snapshot.revert = true;
         DisposableUtils.Dispose(ref _snapshot);
         _snapshot = _get_snapshot();
         IsOpen = false;
         RaisePropertyChanged(nameof(IsOpen));
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
         IsOpen = false;
         RaisePropertyChanged(nameof(IsOpen));
      }

      protected void _destroy_snapshot() =>
         _snapshot = null;

      protected virtual IObjSnapshot _get_snapshot() =>
         detect_property_changes_except(nameof(IsOpen), nameof(IsActive));

      protected void _raise_filter_changed() {
         _active.refresh();
         FilterChanged?.Invoke();
      }

      protected virtual void _validate() { }

      bool IDataGridColumnFilter.Filter(object row) =>
         Filter(ConvertUtils.try_convert<TRow>(row));
   }
}