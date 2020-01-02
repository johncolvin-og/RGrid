using System;
using System.Collections.Generic;

namespace RGrid.Utility {
   class ComparerBuilder<T> {
      public readonly List<IComparer<T>> _comparers_list;

      public ComparerBuilder() =>
         _comparers_list = new List<IComparer<T>>(2);

      public ComparerBuilder(IEnumerable<IComparer<T>> comparers) =>
         _comparers_list = new List<IComparer<T>>(comparers);

      public void add(IComparer<T> comparer) =>
         _comparers_list.Add(comparer);

      public void add<TValue>(Func<T, TValue> get) =>
         add(get, Comparer<TValue>.Default);

      public void add<TValue>(Func<T, TValue> get, IComparer<TValue> comparer) =>
         add(CompareUtils.wrap(comparer, get));

      public void clear() =>
         _comparers_list.Clear();

      public IComparer<T> to_comparer() => new _Comparer(_comparers_list.ToArray());

      class _Comparer : IComparer<T> {
         readonly IComparer<T>[] _comparers;

         public _Comparer(IComparer<T>[] comparers) =>
            _comparers = comparers;

         public int Compare(T x, T y) {
            foreach (var comp in _comparers) {
               int result = comp.Compare(x, y);
               if (result != 0)
                  return result;
            }
            return 0;
         }
      }
   }
}