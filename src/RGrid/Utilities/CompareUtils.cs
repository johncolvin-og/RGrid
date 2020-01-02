using System;
using System.Collections;
using System.Collections.Generic;

namespace RGrid.Utility {
   public static class CompareUtils {
      public static int safe_compare<T>(this T a, T b, Func<T, T, int> compare) =>
         a == null ? (b == null ? 0 : -1) : (b == null ? 1 : compare(a, b));

      public static int safe_compare<T>(this T a, T b) where T : IComparable<T> =>
         a == null ? (b == null ? 0 : -1) : (b == null ? 1 : a.CompareTo(b));

      #region Equality
      public static bool NullCheckEquals<T>(T x, T y) =>
         NullCheckEquals(x, y, (_x, _y) => _x.Equals(_y));

      public static bool NullCheckEquals<T>(T x, T y, Func<T, T, bool> never_null_equals) =>
         x == null ? y == null : y == null ? false : never_null_equals(x, y);
      #endregion

      #region Comparison
      public static Comparison<T> safe_comparison<T>(Func<T, T, int> compare) =>
         (a, b) => a.safe_compare(b, compare);

      public static Comparison<T?> nullable_comparison<T>(Func<T, T, int> compare) where T : struct =>
         (a, b) => a.HasValue ?
            b.HasValue ? compare(a.Value, b.Value) : 1 :
            b.HasValue ? -1 : 0;
      #endregion

      #region Comparer
      public static IComparer to_non_generic<T>(this IComparer<T> comparer) =>
        new NonGenericComparerWrapper<T>(comparer);

      public static IComparer<T> to_safe<T>(this IComparer<T> comparer) where T : class =>
         new SafeComparerWrapper<T>(comparer);

      public static IComparer<TOutter> wrap<TInner, TOutter>(this IComparer<TInner> inner_comparer, Func<TOutter, TInner> selector) =>
         Comparer<TOutter>.Create((a, b) => inner_comparer.Compare(a == null ? default(TInner) : selector(a), b == null ? default(TInner) : selector(b)));

      public static IComparer<T?> to_nullable<T>(this IComparer<T> comparer) where T : struct =>
         Comparer<T?>.Create(nullable_comparison<T>(comparer.Compare));

      public static IComparer<T> reverse<T>(this IComparer<T> comparer) =>
         Comparer<T>.Create((a, b) => comparer.Compare(b, a));

      class NonGenericComparerWrapper<T> : IComparer {
         readonly IComparer<T> _comparer;

         public NonGenericComparerWrapper(IComparer<T> comparer) => _comparer = comparer;

         public int Compare(object x, object y) =>
            _comparer.Compare(ConvertUtils.try_convert<T>(x), ConvertUtils.try_convert<T>(y));
      }

      class SafeComparerWrapper<T> : IComparer<T> where T : class {
         readonly IComparer<T> _comparer;

         public SafeComparerWrapper(IComparer<T> comparer) =>
            _comparer = comparer;

         public int Compare(T x, T y) {
            if (x == null) return y == null ? 0 : -1;
            if (y == null) return 1;
            return _comparer.Compare(x, y);
         }
      }
      #endregion
   }
}