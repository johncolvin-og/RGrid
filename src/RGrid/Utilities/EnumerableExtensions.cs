using EqualityComparer.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;

namespace RGrid.Utility {
   static class EnumerableExtensions {
      /// <summary>
      /// Returns the subsequence of items, in the source sequence, that occur in between <paramref name="item1"/> and <paramref name="item2"/>.  The relative order of said items is irrelevant.
      /// </summary>
      /// <typeparam name="T"></typeparam>
      /// <param name="source">The source sequence used to find a subsequence of items that are in between the specified items.</param>
      /// <param name="item1">One of two items that will serve as a non-inclusive bookend for the resultant sequence.</param>
      /// <param name="item2">One of two items that will serve as a non-inclusive bookend for the resultant sequence.</param>
      /// <returns>The sequence of elements between <paramref name="item1"/> and <paramref name="item2"/>.</returns>
      public static IEnumerable<T> Between<T>(this IEnumerable<T> source, T item1, T item2) {
         using (var en = source.GetEnumerator()) {
            while (en.MoveNext()) {
               if (item1.Equals(en.Current)) {
                  while (en.MoveNext() && !item2.Equals(en.Current))
                     yield return en.Current;
                  yield break;
               } else if (item2.Equals(en.Current)) {
                  while (en.MoveNext() && !item1.Equals(en.Current))
                     yield return en.Current;
                  yield break;
               }
            }
         }
      }

      public static IEnumerable<T> BookEnd<T>(this IEnumerable<T> source, T front_end, T back_end) {
         yield return front_end;
         foreach (var v in source)
            yield return v;
         yield return back_end;
      }

      public static IEnumerable<T> DistinctOn<T, TKey>(this IEnumerable<T> enumerable, Func<T, TKey> key_selector) =>
        enumerable.Distinct(EqualityComparerFactory.Create<T>((x, y) => key_selector(x).Equals(key_selector(y)), x => key_selector(x).GetHashCode()));

      public static IEnumerable<T> ExceptNull<T>(this IEnumerable<T> source) where T : class {
         foreach (var v in source)
            if (v != null)
               yield return v;
      }

      public static bool TryGetFirst<T>(this IEnumerable<T> source, out T result) {
         foreach (var v in source) {
            result = v;
            return true;
         }
         result = default;
         return false;
      }

      public static bool TryGetFirst<T>(this IEnumerable<T> source, Func<T, bool> predicate, out T result) {
         foreach (var v in source) {
            if (predicate(v)) {
               result = v;
               return true;
            }
         }
         result = default;
         return false;
      }

      public static bool TryGetFirst<T>(this IEnumerable<T> source, Func<T, bool> predicate, out T result, out int index) {
         if (source == null) throw new ArgumentNullException(nameof(source));
         index = 0;
         foreach (T element in source) {
            if (predicate(element)) {
               result = element;
               return true;
            }
            index++;
         }
         result = default;
         return false;
      }

      public static double weighted_average<T>(this IEnumerable<T> vals, Func<T, double> value, Func<T, double> weight) {
         double weighted_numerator = vals.Sum(x => value(x) * weight(x));
         double weighted_denominator = vals.Sum(x => weight(x));
         if (weighted_denominator != 0) return weighted_numerator / weighted_denominator;
         return double.NaN;
      }
   }
}