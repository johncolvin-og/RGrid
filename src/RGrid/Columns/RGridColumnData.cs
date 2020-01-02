using RGrid.Filters;
using RGrid.Utility;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace RGrid {
   public class DataGridColumnIDAttribute : Attribute {
      public readonly string id;
      public DataGridColumnIDAttribute(string id) => this.id = id;
   }

   public abstract class DataGridColumnData<T, TColKey, TFiltersState> {
      public readonly ColumnBase col;
      public readonly TColKey key;
      public readonly IDataGridColumnFilter<T> filter;
      public readonly IComparer<T> comparer;

      protected DataGridColumnData(ColumnBase col, TColKey key, IDataGridColumnFilter<T> filter, IComparer<T> comparer) {
         this.col = col;
         this.key = key;
         this.filter = filter;
         this.comparer = comparer;
      }

      public abstract void load_from(TFiltersState state);
      public abstract void assign_to(TFiltersState state);
   }

   public static class DataGridColumnData {
      public static DataGridColumnData<T, TColKey, TFiltersState> create<T, TColKey, TFiltersState, TFilter>(ColumnBase col, IDataGridColumnPersistentFilter<T, TFilter> filter, IComparer<T> comparer) where TColKey : struct {
         TColKey key = ExceptionAssert.Argument.Is<IKeyedColumn<TColKey>>(col, nameof(col)).key;
         var prop = typeof(TFiltersState).GetProperties().FirstOrDefault(pi => {
            var id_attr = pi.GetCustomAttribute<DataGridColumnIDAttribute>();
            return id_attr != null && EnumHelper.parse<TColKey>(id_attr.id).Equals(key);
         });
         if (prop == null)
            throw new ArgumentException($"No property in type {typeof(TFiltersState)} has a {nameof(DataGridColumnIDAttribute)} with the specified id '{key}.'");
         return new Impl<T, TColKey, TFiltersState, TFilter>(col, key, filter, comparer, fs => ConvertUtils.try_convert<TFilter>(prop.GetValue(fs)), (fs, f) => prop.SetValue(fs, f));
      }

      class Impl<T, TColKey, TFiltersState, TFilter> : DataGridColumnData<T, TColKey, TFiltersState> {
         readonly Func<TFiltersState, TFilter> _get_filter;
         readonly Action<TFiltersState, TFilter> _set_filter;

         public Impl(ColumnBase col, TColKey key, IDataGridColumnPersistentFilter<T, TFilter> filter, IComparer<T> comparer, Func<TFiltersState, TFilter> get_filter, Action<TFiltersState, TFilter> set_filter)
            : base(col, key, filter, comparer) {
            this.filter = filter;
            _get_filter = get_filter;
            _set_filter = set_filter;
         }

         public new IDataGridColumnPersistentFilter<T, TFilter> filter { get; }

         public override void assign_to(TFiltersState state) =>
            _set_filter(state, filter.GetState());

         public override void load_from(TFiltersState state) =>
            filter.LoadState(_get_filter(state));
      }
   }
}