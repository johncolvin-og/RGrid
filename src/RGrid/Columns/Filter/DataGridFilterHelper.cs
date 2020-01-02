using Disposable.Extensions;
using RGrid.Utility;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;

namespace RGrid.Filters {
   static class DataGridFilterHelper {
      public static IDisposable hook_filters<TRow>(ObservableFilterCollection<TRow> rows, Action filter_action, params IDataGridColumnFilter<TRow>[] filters) =>
         _hook_filters(filter_action, filters);

      public static IDisposable hook_filters<TRow>(ObservableFilterCollection<TRow> rows, params IDataGridColumnFilter<TRow>[] filters) {
         return _hook_filters(set_filter, filters);
         void set_filter() {
            using (rows.defer_refresh()) {
               var active_filters = _get_active_filters(filters);
               rows.live_filter_properties.SetItems(active_filters.Select(f => f.prop_name));
               rows.filter = r => active_filters.All(f => f.Filter(r));
            }
         }
      }

      public static IDisposable hook_filters<TRow>(ICollectionView rows, params IDataGridColumnFilter<TRow>[] filters) {
         var icvls = (ICollectionViewLiveShaping)rows;
         icvls.IsLiveFiltering = true;
         return _hook_filters(set_filter, filters);

         void set_filter() {
            using (rows.DeferRefresh()) {
               var active_filters = _get_active_filters(filters);
               icvls.LiveFilteringProperties.sync_with(active_filters.Select(f => f.prop_name));
               rows.Filter = r_obj => {
                  TRow r = (TRow)r_obj;
                  return active_filters.All(f => f.Filter(r));
               };
            }
         }
      }

      static IDisposable _hook_filters<TRow>(Action set_filter, params IDataGridColumnFilter<TRow>[] filters) {
         foreach (var f in filters)
            f.filter_changed += set_filter;
         set_filter();
         return DisposableFactory.Create(() => {
            foreach (var f in filters) {
               f.filter_changed -= set_filter;
               (f as IDisposable)?.Dispose();
            }
         });
      }

      static List<IDataGridColumnFilter<TRow>> _get_active_filters<TRow>(IDataGridColumnFilter<TRow>[] filters) =>
         filters.Where(f => f.active.HasValue && f.active.Value).ToList();
   }

   public static class DataGridFilterKnownTemplates {
      public const string
         DoubleMinMax = "DoubleMinMaxTemplate",
         IntMinMax = "IntMinMaxTemplate",
         UIntMinMax = "UIntMinMaxTemplate",
         UShortMinMax = "UShortMinMaxTemplate",
         ULongMinMax = "ULongMinMaxTemplate",
         TimeFilter = "TimeFilterTemplate",
         DiscreteValues = "DiscreteValuesTemplate",
         EnumValues = "EnumValuesTemplate",
         Regex = "RegexTemplate";
   }
}