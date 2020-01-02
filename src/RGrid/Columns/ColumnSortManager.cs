using Collections.Sync;
using Disposable.Extensions;
using RGrid.Controls;
using RGrid.Filters;
using RGrid.Utility;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.ComponentModel;
using System.Linq;
using System.Reactive.Linq;

namespace RGrid {
   class ColumnSortManager {
      readonly List<ColumnBase> _visible_sorting = new List<ColumnBase>();
      readonly IList<ColumnBase> _columns;
      readonly string _primary_key;

      public ColumnSortManager(IList<ColumnBase> columns, string primary_key) {
         _columns = columns;
         _primary_key = primary_key;
      }

      public IEnumerable<ColumnBase> ActiveSorting {
         get {
            bool has_primary = false;
            foreach (var col in _visible_sorting) {
               if (!has_primary && col.ID == _primary_key) {
                  has_primary = true;
               }
               yield return col;
            }
            if (!has_primary) {
               var pcol = _find_column(_primary_key);
               if (pcol != null)
                  yield return pcol;
            }
         }
      }

      public IEnumerable<ColumnBase> VisibleSorting =>
         _visible_sorting.AsEnumerable();

      public IEnumerable<SortingCriteria> SortingCriteria {
         get {
            foreach (var col in _visible_sorting) {
               var dir = col.SortDirection;
               if (dir.HasValue)
                  yield return new SortingCriteria(col.ID, dir.Value == ListSortDirection.Ascending);
            }
         }
         set {
            _clear();
            if (value != null) {
               foreach (var sc in value) {
                  var col = _find_column(sc.col_name);
                  if (col != null) {
                     col.SortDirection = sc.direction();
                     _visible_sorting.Add(col);
                  }
               }
            }
         }
      }

      public void OnColumnClick(string id, bool is_shift_pressed) =>
         OnColumnClick(_find_column(id), is_shift_pressed);

      public void OnColumnClick(ColumnBase col, bool is_shift_pressed) {
         if (col == null)
            return;
         var old_dir = col.SortDirection;
         if (!is_shift_pressed) {
            _clear();
            col.SortDirection = _reverse(old_dir);
            _visible_sorting.Add(col);
         } else {
            col.SortDirection = _reverse(old_dir);
            if (!old_dir.HasValue)
               _visible_sorting.Add(col);
         }
      }

      static ListSortDirection _reverse(ListSortDirection? dir) =>
         dir.HasValue && dir.Value == ListSortDirection.Ascending ? ListSortDirection.Descending : ListSortDirection.Ascending;

      void _clear() {
         _visible_sorting.Clear();
         foreach (var c in _columns)
            c.SortDirection = null;
      }

      ColumnBase _find_column(string id) =>
         _columns.FirstOrDefault(c => c.ID == id);
   }

   static class ColumnSortManagerExtensions {
      public static IComparer<TRow> BuildComparer<TColumnKey, TRow>(
         this ColumnSortManager sort_manager,
         Func<TColumnKey, IComparer<TRow>> get_column_comparer)
         where TColumnKey : struct {
         //
         var bd = new ComparerBuilder<TRow>();
         foreach (var col in sort_manager.ActiveSorting) {
            if (Enum.TryParse(col.ID, true, out TColumnKey key)) {
               var dir = col.SortDirection;
               if (dir.HasValue) {
                  var comp = get_column_comparer(key);
                  if (comp != null)
                     bd.add(_reverse_if_descending(comp, dir.Value));
               }
            }
         }
         return bd.to_comparer();
      }

      static IComparer<T> _reverse_if_descending<T>(IComparer<T> comp, ListSortDirection direction) =>
         direction == ListSortDirection.Descending ? comp.reverse() : comp;
   }

   class ColumnFilterManager {
      public static IObservable<Func<T, bool>> SubscribeFilter<T>(IReadOnlyObservableCollection<ColumnBase> columns) {
         return Observable.Create<Func<T, bool>>(o => {
            var filter_map = columns.OfType<IFilterableColumn<T>>()
               .Where(fc => fc.filter.active ?? false)
               .ToDictionary(fc => fc.ID);
            return ObservableAutoWrapper.ConnectItemHooks(columns, c => {
               if (c is IFilterableColumn<T> fc) {
                  fc.filter.filter_changed += on_filter_changed;
                  return DisposableFactory.Create(() => fc.filter.filter_changed -= on_filter_changed);
                  void on_filter_changed() {
                     if (fc.filter.active ?? false) {
                        filter_map[c.ID] = fc;
                     } else {
                        filter_map.Remove(c.ID);
                     }
                     // Don't reference the singular filter_map in the predicate;
                     // each broadcasted predicate must be immutable.
                     var active_filters = filter_map.Values.Select(_fc => _fc.filter).ToList();
                     o.OnNext(r => active_filters.All(f => f.Filter(r)));
                  }
               }
               return null;
            });
         });
      }
   }
}
