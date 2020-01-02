using Disposable.Extensions;
using EqualityComparer.Extensions;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;

namespace RGrid.Utility {
   static class GlobalExtensions {
      public static string fmt(this string str, params object[] args) =>
         string.Format(str, args);

      public static void difference<T>(this IEnumerable<T> current, IEnumerable<T> newval, out IEnumerable<T> added, out IEnumerable<T> removed) { current.difference(newval, out added, out removed, EqualityComparer<T>.Default); }
      public static void difference<T>(this IEnumerable<T> current, IEnumerable<T> newval, out IEnumerable<T> added, out IEnumerable<T> removed, IEqualityComparer<T> comp) {
         added = newval.Except(current, comp);
         removed = current.Except(newval, comp);
      }

      public static int IndexOf(this IEnumerable source, object obj) =>
         IndexOf(source, obj, EqualityComparer<object>.Default.ToNonGeneric());

      public static int IndexOf(this IEnumerable source, object obj, IEqualityComparer comparer) {
         int i = 0;
         foreach (var v in source) {
            if (comparer.Equals(v, obj))
               return i;
            ++i;
         }
         return -1;
      }

      public static int IndexOf<T>(this IEnumerable<T> source, T value, IEqualityComparer<T> comparer) {
         int i = 0;
         foreach (var v in source) {
            if (comparer.Equals(v, value))
               return i;
            ++i;
         }
         return -1;
      }

      public static void ApplyToFirst<T>(this IEnumerable<T> source, Func<T, bool> where, Action<T> action) {
         foreach (var v in source) {
            if (where(v))
               action(v);
         }
      }

      public static void TryExecuteIfCan(this ICommand command, object parameter) =>
         command?.ExecuteIfCan(parameter);

      public static void ExecuteIfCan(this ICommand command, object parameter) {
         if (command.CanExecute(parameter))
            command.Execute(parameter);
      }

      public static void removeRange<T>(this IList<T> list, IEnumerable<T> items) {
         foreach (T v in items)
            list.Remove(v);
      }

      //this overloaded declaration, (as opposed to one function with comp = null) allows observable_collection.sync_with to be passed into functions as a parameterless Action
      public static void sync_with<T>(this IList<T> dest, IEnumerable<T> src) { dest.sync_with(src, EqualityComparer<T>.Default); }
      public static void sync_with<T>(this IList<T> dest, IEnumerable<T> src, IEqualityComparer<T> comp) {
         if (src == null) {
            dest.Clear();
            return;
         }
         var srcList = src as IList<T> ?? src.ToList();
         var changes = bts.utils.Diff.diff(dest, srcList, comp).ToList();
         // Apply the diff
         for (int i = 0; i < changes.Count; ++i) {
            var c = changes[i];
            // removes
            for (var x = 0; x < c.deletedLeft; ++x) {
               dest.RemoveAt(c.StartLeft);
            }
            // inserts
            for (var x = 0; x < c.insertedRight; ++x) {
               dest.Insert(c.StartLeft + x, srcList[c.StartRight + x]);
            }

            // Adjust start index of remaining changes
            for (int ii = i + 1; ii < changes.Count; ++ii) {
               var cc = changes[ii];
               var delta = c.insertedRight - c.deletedLeft;
               if (cc.StartLeft >= c.StartLeft) {
                  cc.StartLeft += delta;
                  changes[ii] = cc;//remember: struct
               }
            }
         }
      }

      public static bool IsNullable(this Type type) {
         if (!type.IsGenericType) {
            return false;
         }
         if (type.GetGenericTypeDefinition() == typeof(Nullable<>)) {
            return true;
         } else {
            return false;
         }
      }

      public static IReadOnlyList<T> ToReadOnlyList<T>(this IEnumerable<T> source) =>
         source.ToList();

      public static void RemoveRangeAt<T>(this IList<T> list, int index, int count) {
         for (int i = 0; i < count; i++)
            list.RemoveAt(index);
      }

      public static void AddRange<T>(this IList<T> list, IEnumerable<T> items) {
         foreach (T item in items)
            list.Add(item);
      }

      public static void AddRange<T>(this ISet<T> list, IEnumerable<T> items) {
         foreach (T item in items)
            list.Add(item);
      }

      public static void SetItems<T>(this IList<T> list, IEnumerable<T> items) {
         list.Clear();
         list.AddRange(items);
      }

      public static void SetItems<T>(this ISet<T> set, IEnumerable<T> items) {
         set.Clear();
         set.AddRange(items);
      }

      public static IEnumerable<T> Except<T, TKey>(this IEnumerable<T> enumerable, IEnumerable<TKey> second, Func<T, TKey> key_selector) =>
        enumerable.Where(t => !second.Contains(key_selector(t)));

      public static IEnumerable<T> Intersect<T, TKey>(this IEnumerable<T> enumerable, IEnumerable<TKey> second, Func<T, TKey> key_selector) =>
         enumerable.Where(t => second.Contains(key_selector(t)));

      public static IDisposable Subscribe(this INotifyCollectionChanged source, NotifyCollectionChangedEventHandler callback) {
         source.CollectionChanged += callback;
         return DisposableFactory.Create(() => source.CollectionChanged -= callback);
      }
      public static IDisposable SuspendHandler(this INotifyCollectionChanged source, NotifyCollectionChangedEventHandler callback) {
         source.CollectionChanged -= callback;
         return DisposableFactory.Create(() => source.CollectionChanged += callback);
      }

      public static bool is_null_or_empty<T>(this IEnumerable<T> source) =>
         source == null || !source.Any();

      public static IEnumerable<T> empty_if_null<T>(this IEnumerable<T> source) =>
         source ?? Enumerable.Empty<T>();

      public static TValue? GetNullable<TKey, TValue>(this IDictionary<TKey, TValue> dictionary, TKey key) where TValue : struct {
         if (dictionary.TryGetValue(key, out TValue result))
            return result;
         else return null;
      }

      public static void AddOrUpdate<TKey, TValue>(this IDictionary<TKey, TValue> dictionary, TKey key, Action<TValue> update_existing, Func<TValue> create_new) {
         if (dictionary.TryGetValue(key, out TValue curr)) update_existing(curr);
         else dictionary.Add(key, create_new());
      }

      public static void ignore(this Task task) { }
   }
}