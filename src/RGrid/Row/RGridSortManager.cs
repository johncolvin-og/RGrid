using Collections.Sync;
using Disposable.Extensions;
using RGrid.Utility;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;

namespace RGrid {
   public class RGridSortManager<TColKey> : IDisposable where TColKey : struct {
      readonly ObservableKeyedCollection<(RGrid.ColumnBase col, TColKey key, ListSortDirection direction), TColKey> _active_cols = new ObservableKeyedCollection<(RGrid.ColumnBase col, TColKey key, ListSortDirection direction), TColKey>(t => t.key);
      readonly IReadOnlyDictionary<TColKey, (RGrid.ColumnBase col, TColKey)> _map;
      readonly TColKey _primary_key;
      readonly DeferrableActionWrapper _raise_sorting_changed;
      IDisposable _dispose;

      public RGridSortManager(IReadOnlyObservableKeyedCollection<(RGrid.ColumnBase col, TColKey key), TColKey> map, TColKey primary_key) {
         _primary_key = primary_key;
         _map = map;
         _raise_sorting_changed = new DeferrableActionWrapper(() => sorting_changed?.Invoke());
         _dispose = DisposableFactory.Create(
            ObservableAutoWrapper.ConnectItemHooks(map, t => {
               t.col.sort_direction_changed += on_sort_direction_changed;
               return DisposableFactory.Create(() => t.col.sort_direction_changed -= on_sort_direction_changed);
               //
               void on_sort_direction_changed(RGrid.ColumnBase col, ListSortDirection? direction) {
                  if (direction.HasValue) {
                     _active_cols[t.key] = (col, t.key, direction.Value);
                  } else
                     _active_cols.Remove(t.key);
               }
            }),
            _active_cols.Subscribe((s, e) => _raise_sorting_changed.execute()));
      }

      public event Action sorting_changed;

      public void build_comparer<T>(Func<TColKey, IComparer<T>> get_comparer_for_column, out IReadOnlyList<(TColKey col, ListSortDirection direction)> column_params, out IComparer<T> comparer) {
         var mcolumn_params = _active_cols.AsCollection().Select(t => (t.key, t.direction)).ToList();
         if (!_active_cols.ContainsKey(_primary_key)) {
            var pcol = _map[_primary_key];
            mcolumn_params.Add((_primary_key, ListSortDirection.Ascending));
         }
         column_params = mcolumn_params;
         comparer = new ComparerBuilder<T>(mcolumn_params.Select(t =>
             t.direction == ListSortDirection.Ascending ?
                get_comparer_for_column(t.key) :
                get_comparer_for_column(t.key).reverse())
         ).to_comparer();
      }

      public void load_from(IEnumerable<SortingCriteria> sorting_criterion) =>
         _on_settings(sorting_criterion.Select(sc => (EnumHelper.parse<TColKey>(sc.col_name), sc.is_ascending ? ListSortDirection.Ascending : ListSortDirection.Descending)));

      public IEnumerable<SortingCriteria> create() =>
         _active_cols.AsCollection().Select(t => new SortingCriteria(t.col.ID, t.direction == ListSortDirection.Ascending));

      public void on_sorting(DataGridSortingEventArgs e) =>
         on_sorting(e, !KeyboardExtensions.IsShiftDown());

      public void on_sorting(DataGridSortingEventArgs e, bool clear_current) {
         e.Handled = true;
         var k = _key(e.Column);
         using (_raise_sorting_changed.defer_execution()) {
            var newdir = e.Column.SortDirection.HasValue && e.Column.SortDirection.Value == ListSortDirection.Ascending ? ListSortDirection.Descending : ListSortDirection.Ascending;
            if (clear_current) {
               var removes = _active_cols.AsCollection().ToList();
               foreach (var (col, key, dir) in removes)
                  col.SortDirection = null;
            }
            e.Column.SortDirection = newdir;
         }
      }

      public void Dispose() => _dispose.Dispose();

      static TColKey _key(RGrid.ColumnBase column) =>
         ((RGrid.IKeyedColumn<TColKey>)column).key;

      void _on_settings(IEnumerable<(TColKey col, ListSortDirection direction)> sort_settings) {
         using (_raise_sorting_changed.defer_execution()) {
            var removes = _active_cols.AsCollection().ToList();
            foreach (var (col, key, direction) in removes)
               col.SortDirection = null;
            foreach (var (col, direction) in sort_settings.DistinctOn(s => s.col)) {
               if (_map.TryGetValue(col, out (RGrid.ColumnBase col, TColKey key) match))
                  match.col.SortDirection = direction;
            }
         }
      }
   }
}