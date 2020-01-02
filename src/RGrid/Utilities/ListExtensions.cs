using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace RGrid.Collections {
   public static class ListExtensions {
      public static void RemoveWhere<T>(this IList<T> list, Func<T, bool> where) { foreach (var r in list.Where(where).ToList()) list.Remove(r); }

      public static void RemoveWhere<T>(this IList<T> list, Func<T, bool> where, Action<T> on_remove) {
         int count = list.Count;
         for (int i = 0; i < count; i++) {
            var t = list[i];
            if (where(t)) {
               list.RemoveAt(i--);
               --count;
               on_remove(t);
            }
         }
      }

      public static IList<T> WrapType<T>(this IList list) =>
         new TypeWrapper<T>(list);

      public static IList<T> WrapSubRange<T>(this IList<T> list, int start, int length) =>
         new SubRangeWrapper<T>(list, start, length);

      public static IList<T> WrapSelector<T>(this IList list, Func<object, T> convert, Func<T, object> convert_back = null) =>
         new SelectorWrapper<T>(list, convert, convert_back);

      public static IList WrapSelectorHidden<T>(this IList list, Func<object, T> convert, Func<T, object> convert_back = null) =>
         new SelectorWrapper<T>(list, convert, convert_back);

      public static IList<TResult> WrapSelector<T, TResult>(this IList<T> list, Func<T, TResult> convert, Func<TResult, T> convert_back) =>
         new SelectorWrapper<T, TResult>(list, convert, convert_back);

      public static IReadOnlyList<TResult> WrapSelectorRO<T, TResult>(this IList<T> list, Func<T, TResult> convert) =>
         new SelectorWrapper<T, TResult>(list, convert, null);

      private static Random rng = new Random();

      public static void Shuffle<T>(this IList<T> list) {
         int n = list.Count;
         while (n > 1) {
            n--;
            int k = rng.Next(n + 1);
            T value = list[k];
            list[k] = list[n];
            list[n] = value;
         }
      }

      public static void RemoveAt<T>(this IList<T> list, int index, int count) {
         for (int i = 0; i < count; i++)
            list.RemoveAt(index);
      }

      public static int IndexOf<T>(IReadOnlyList<T> list, T value) {
         for (int i = 0; i < list.Count; i++)
            if (value.Equals(list[i]))
               return i;
         return -1;
      }

      class TypeWrapper<T> : IList<T>, IReadOnlyList<T> {
         readonly IList _source;

         public TypeWrapper(IList source) =>
            _source = source;

         public T this[int index] {
            get => _source[index] is T v ? v : default(T);
            set => _source[index] = value;
         }
         public int Count => _source.Count;
         public bool IsReadOnly => _source.IsReadOnly;

         public void Add(T item) => _source.Add(item);
         public void Clear() => _source.Clear();
         public bool Contains(T item) => _source.Contains(item);
         public void CopyTo(T[] array, int array_index) => _source.CopyTo(array, array_index);
         public IEnumerator<T> GetEnumerator() {
            for (int i = 0; i < _source.Count; i++)
               yield return _source[i] is T v ? v : default(T);
         }
         public int IndexOf(T item) => _source.IndexOf(item);
         public void Insert(int index, T item) => _source.Insert(index, item);
         public bool Remove(T item) {
            int index = _source.IndexOf(item);
            if (index >= 0) {
               _source.RemoveAt(index);
               return true;
            } else return false;
         }
         public void RemoveAt(int index) => _source.RemoveAt(index);
         IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
      }

      class SubRangeWrapper<T> : IList<T>, IReadOnlyList<T> {
         readonly IList<T> _source;
         readonly int _start;

         public SubRangeWrapper(IList<T> source, int start, int count) {
            _source = source;
            _start = start;
            Count = count;
         }

         public T this[int index] {
            get => _source[_convert_index(index)];
            set => _source[_convert_index(index)] = value;
         }
         public int Count { get; private set; }
         public bool IsReadOnly => _source.IsReadOnly;

         public void Add(T item) {
            int index = _start + Count;
            if (index < _source.Count) {
               _source.Insert(index, item);
            } else _source.Add(item);
            Count++;
         }
         public void Clear() {
            if (_source is List<T> l) {
               /// Favor <see cref="List{T}.RemoveRange(int, int)"/>; it is optimized.
               l.RemoveRange(_start, Count);
               Count = 0;
            } else {
               while (Count > 0) {
                  _source.RemoveAt(_start);
                  Count--;
               }
            }
         }
         public bool Contains(T item) => IndexOf(item) >= 0;
         public void CopyTo(T[] array, int arrayIndex) {
            int i = 0;
            foreach (T v in this)
               array[arrayIndex + i] = v;
         }
         public IEnumerator<T> GetEnumerator() {
            int stop = _start + Count;
            for (int i = _start; i < stop; i++)
               yield return _source[i];
         }
         public int IndexOf(T item) {
            int i = 0;
            foreach (T v in this) {
               if (v.Equals(item))
                  return i;
               i++;
            }
            return -1;
         }
         public void Insert(int index, T item) {
            _source.Insert(_convert_index(index), item);
            Count++;
         }
         public bool Remove(T item) {
            int i = IndexOf(item);
            if (i >= 0) {
               _source.RemoveAt(i);
               Count--;
               return true;
            }
            return false;
         }
         public void RemoveAt(int index) {
            _source.RemoveAt(_convert_index(index));
            Count--;
         }
         IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

         int _convert_index(int index) {
            if (index >= Count || index < 0) throw new ArgumentOutOfRangeException(nameof(index));
            return _start + index;
         }
      }

      class SelectorWrapper<T> : IList<T>, IList, IReadOnlyList<T> {
         readonly Func<object, T> _convert;
         readonly Func<T, object> _convert_back;
         readonly IList _source;

         public SelectorWrapper(IList source, Func<object, T> convert, Func<T, object> convert_back) {
            _source = source;
            _convert = convert;
            _convert_back = convert_back;
         }

         public T this[int index] {
            get => _convert(_source[index]);
            set => _source[index] = _convert_back(value);
         }
         public int Count => _source.Count;
         public bool IsReadOnly => _source.IsReadOnly && _convert_back != null;

         public void Add(T item) => _source.Add(_convert_back(item));
         public void Clear() => _source.Clear();
         public bool Contains(T item) => _source.Contains(_convert_back(item));
         public void CopyTo(T[] array, int arrayIndex) {
            for (int i = 0; i < _source.Count; i++)
               array[arrayIndex + i] = _convert(_source[i]);
         }
         public IEnumerator<T> GetEnumerator() {
            for (int i = 0; i < _source.Count; i++)
               yield return _convert(_source[i]);
         }
         public int IndexOf(T item) => _source.IndexOf(_convert_back(item));
         public void Insert(int index, T item) => _source.Insert(index, _convert_back(item));
         public bool Remove(T item) {
            int i = _source.IndexOf(_convert_back(item));
            if (i >= 0) {
               _source.RemoveAt(i);
               return true;
            } else return false;
         }
         public void RemoveAt(int index) => _source.RemoveAt(index);

         #region IList (non-generic)
         object IList.this[int index] {
            get => this[index];
            set {
               if (value is T v)
                  this[index] = v;
            }
         }
         bool IList.IsFixedSize => _source.IsFixedSize;
         bool IList.IsReadOnly => IsReadOnly;
         bool ICollection.IsSynchronized => _source.IsSynchronized;
         object ICollection.SyncRoot => _source.SyncRoot;
         int IList.Add(object value) {
            if (value is T v) {
               return _source.Add(_convert_back(v));
            } else throw new ArgumentException(nameof(value), $"Unexpected type '{value?.GetType()}.'");
         }
         bool IList.Contains(object value) => value is T v && _source.Contains(_convert_back(v));
         void ICollection.CopyTo(Array array, int index) {
            for (int i = 0; i < _source.Count; i++)
               array.SetValue(_convert(_source[index]), index + i);
         }
         IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
         int IList.IndexOf(object value) => value is T v ? IndexOf(v) : -1;
         void IList.Insert(int index, object value) {
            if (value is T v)
               Insert(index, v);
         }
         void IList.Remove(object value) {
            if (value is T v)
               Remove(v);
         }
         void IList.RemoveAt(int index) => RemoveAt(index);
         #endregion
      }

      class SelectorWrapper<TIn, TOut> : IList<TOut>, IReadOnlyList<TOut> {
         readonly Func<TIn, TOut> _convert;
         readonly Func<TOut, TIn> _convert_back;
         readonly IList<TIn> _source;

         public SelectorWrapper(IList<TIn> source, Func<TIn, TOut> convert, Func<TOut, TIn> convert_back) {
            _source = source;
            _convert = convert;
            _convert_back = convert_back;
         }

         public TOut this[int index] {
            get => _convert(_source[index]);
            set => _source[index] = _convert_back(value);
         }
         public int Count => _source.Count;
         public bool IsReadOnly => _source.IsReadOnly && _convert_back != null;
         public void Add(TOut item) => _source.Add(_convert_back(item));
         public void Clear() => _source.Clear();
         public bool Contains(TOut item) => _source.Contains(_convert_back(item));
         public void CopyTo(TOut[] array, int arrayIndex) {
            for (int i = 0; i < _source.Count; i++)
               array[arrayIndex + i] = _convert(_source[i]);
         }
         public IEnumerator<TOut> GetEnumerator() {
            for (int i = 0; i < _source.Count; i++)
               yield return _convert(_source[i]);
         }
         public int IndexOf(TOut item) => _source.IndexOf(_convert_back(item));
         public void Insert(int index, TOut item) => _source.Insert(index, _convert_back(item));
         public bool Remove(TOut item) => _source.Remove(_convert_back(item));
         public void RemoveAt(int index) => _source.RemoveAt(index);
         IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
      }
   }
}