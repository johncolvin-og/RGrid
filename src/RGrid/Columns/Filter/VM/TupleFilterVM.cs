using RGrid.WPF;
using System;
using System.Windows.Input;

namespace RGrid.Filters {
   class TupleFilter<TRow, TValue1, TState1, TValue2, TState2, TCompoundState> : ViewModelBaseWithValidation, IDataGridColumnPersistentFilter<TRow, TCompoundState>, IRequestBindingSourceUpdate {
      readonly FilterVMBase<TRow, TValue1, TState1> _filter1;
      readonly FilterVMBase<TRow, TValue2, TState2> _filter2;
      readonly Func<TState1, TState2, TCompoundState> _get_state;
      readonly Func<TCompoundState, TState1> _get_filter1_state;
      readonly Func<TCompoundState, TState2> _get_filter2_state;

      public TupleFilter(
         FilterVMBase<TRow, TValue1, TState1> filter1,
         Func<TCompoundState, TState1> get_filter1_state,
         FilterVMBase<TRow, TValue2, TState2> filter2,
         Func<TCompoundState, TState2> get_filter2_state,
         Func<TState1, TState2, TCompoundState> get_state) {
         //Logger.assert(filter1.prop_name == filter2.prop_name);
         prop_name = filter1.prop_name;
         _filter1 = filter1;
         _filter2 = filter2;
         _get_filter1_state = get_filter1_state;
         _get_filter2_state = get_filter2_state;
         _get_state = get_state;
         _filter1.FilterChanged += () => FilterChanged?.Invoke();
         _filter2.FilterChanged += () => FilterChanged?.Invoke();
      }

      public event Action FilterChanged;
      public event Action RequestUpdate;

      public FilterVMBase<TRow, TValue1, TState1> filter1 => _filter1;
      public FilterVMBase<TRow, TValue2, TState2> filter2 => _filter2;
      public string prop_name { get; }
      public bool? IsActive {
         get {
            bool? f1_active = _filter1.IsActive;
            if (f1_active.HasValue) {
               bool? f2_active = _filter2.IsActive;
               if (f2_active.HasValue)
                  return f1_active.Value && f2_active.Value;
            }
            return null;
         }
      }
      public bool IsOpen { get; set; }
      public ICommand Apply { get; }
      public ICommand Clear { get; }
      public ICommand Cancel { get; }

      public bool Filter(TRow row) {
         if (!IsActive.HasValue || !IsActive.Value)
            return true;
         return (!_filter1.IsActive.Value || _filter1.Filter(row)) && (!_filter2.IsActive.Value || _filter2.Filter(row));
      }

      public TCompoundState GetState() =>
         _get_state(_filter1.GetState(), _filter2.GetState());

      public void LoadState(TCompoundState state) {
         _filter1.LoadState(_get_filter1_state(state));
         _filter2.LoadState(_get_filter2_state(state));
      }

      protected void _raise_request_update() =>
         RequestUpdate?.Invoke();


      bool IDataGridColumnFilter.Filter(object row) =>
         Filter(ConvertUtils.try_convert<TRow>(row));
   }
}