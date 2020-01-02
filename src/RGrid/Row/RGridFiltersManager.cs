using Collections.Sync;
using Disposable.Extensions;
using RGrid.Filters;
using RGrid.Utility;
using System;
using System.Collections.Generic;
using System.Linq;

namespace RGrid {
   public interface IValueFactory<T> {
      T create();
      void load_from(T value);
   }

   public interface IRGridFiltersManager<T, TColKey, TFiltersState> : IValueFactory<TFiltersState> {
      event Action active_filters_changed;
      IEnumerable<(TColKey col, IDataGridColumnFilter<T> filter)> active_col_filters { get; }
   }

   static class RGridFiltersManager {
      public static IRGridFiltersManager<T, TColKey, TFiltersState> create<T, TColKey, TFiltersState>(ObservableKeyedCollection<DataGridColumnData<T, TColKey, TFiltersState>, TColKey> columns) where TFiltersState : new() where TColKey : struct =>
         new Impl<T, TColKey, TFiltersState>(columns, () => new TFiltersState());

      class Impl<T, TColKey, TFiltersState> : IRGridFiltersManager<T, TColKey, TFiltersState> {
         readonly ObservableKeyedCollection<DataGridColumnData<T, TColKey, TFiltersState>, TColKey> _columns;
         readonly DeferrableActionWrapper _raise_filters_changed;
         readonly Func<TFiltersState> _filters_state_factory;

         public Impl(ObservableKeyedCollection<DataGridColumnData<T, TColKey, TFiltersState>, TColKey> columns, Func<TFiltersState> filters_state_factory) {
            _columns = columns;
            _filters_state_factory = filters_state_factory;
            _raise_filters_changed = new DeferrableActionWrapper(() => active_filters_changed?.Invoke());
            ObservableAutoWrapper.ConnectItemHooks(_columns.AsCollection(), f => {
               f.filter.filter_changed += _raise_filters_changed.execute;
               return DisposableFactory.Create(() => f.filter.filter_changed -= _raise_filters_changed.execute);
            });
         }

         public event Action active_filters_changed;
         public IEnumerable<(TColKey col, IDataGridColumnFilter<T> filter)> active_col_filters => _columns.AsCollection().Where(f => f.filter.active ?? false).Select(f => (f.key, f.filter));

         public void add_column(DataGridColumnData<T, TColKey, TFiltersState> col) {
            _columns[col.key] = col;
         }

         public TFiltersState create() {
            var fs = _filters_state_factory();
            foreach (var f in _columns)
               f.assign_to(fs);
            return fs;
         }

         public void load_from(TFiltersState fs) {
            using (_raise_filters_changed.defer_execution()) {
               foreach (var f in _columns)
                  f.load_from(fs);
            }
         }
      }
   }
}