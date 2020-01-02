using RGrid.Utility;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;

namespace RGrid {
   public class RGridRowsManager<T, TColKey, TFiltersState> where TColKey : struct {
      readonly IRGridFiltersManager<T, TColKey, TFiltersState> _filters;
      readonly RGridSortManager<TColKey> _sorting;
      readonly DeferrableActionWrapper _update;
      bool _filters_dirty, _sorting_dirty;

      public RGridRowsManager(IRGridFiltersManager<T, TColKey, TFiltersState> filters_manager, RGridSortManager<TColKey> sort_manager, Func<TColKey, IComparer<T>> get_comparer_for_column, Action<Func<T, bool>> set_filter, Action<IComparer<T>> set_sorting, Action<IComparer<T>, Func<T, bool>> reset, string window_name) {
         _filters = filters_manager;
         _sorting = sort_manager;
         _update = new DeferrableActionWrapper(() => {
            if (_filters_dirty) {
               if (_sorting_dirty) {
                  var comp = build_comparer(out string comp_desc);
                  var filter = build_filter(out string filter_desc);
                  //Log.Ui.info($"{window_name} begin filter '{filter_desc}' and sort '{comp_desc}.'");
                  reset(comp, filter);
               } else {
                  var filter = build_filter(out string filter_desc);
                  //Log.Ui.info($"{window_name} begin filter '{filter_desc}.'");
                  set_filter(filter);
               }
            } else if (_sorting_dirty) {
               var comp = build_comparer(out string comp_desc);
               //Log.Ui.info($"{window_name} begin sort '{comp_desc}.'");
               set_sorting(comp);
            }
            _sorting_dirty = false;
            _filters_dirty = false;
         });
         _filters.active_filters_changed += on_filters_changed;
         _sorting.sorting_changed += on_sorting_changed;
         // local methods
         void on_filters_changed() {
            _filters_dirty = true;
            _update.execute();
         }
         void on_sorting_changed() {
            _sorting_dirty = true;
            _update.execute();
         }
         IComparer<T> build_comparer(out string desc) {
            _sorting.build_comparer(get_comparer_for_column, out IReadOnlyList<(TColKey col, ListSortDirection direction)> col_params, out IComparer<T> comp);
            desc = string.Join(", ", col_params.Select(sd => $"{sd.col}:{sd.direction}"));
            return comp;
         }
         Func<T, bool> build_filter(out string desc) {
            var col_filters = filters_manager.active_col_filters.ToArray();
            if (col_filters.Length == 0) {
               desc = "(none)";
               return null;
            } else {
               // TODO: the actual IDataGridFilter implementations should be able to provide a concise description of their filter state/parameters (e.g., for a min/max filter: 'min=40, max=60')
               desc = string.Join(", ", col_filters.Select(f => f.col));
               var filters = Array.ConvertAll(col_filters, cf => cf.filter);
               return new Func<T, bool>(r => filters.All(f => f.Filter(r)));
            }
         }
      }

      public void on_settings(TFiltersState filters, IEnumerable<SortingCriteria> sorting) {
         using (_update.defer_execution()) {
            _filters.load_from(filters);
            _sorting.load_from(sorting);
         }
      }
   }
}