using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;

namespace RGrid.Utility {
   class ObservableSortedCollection<T> : ObservableCollection<T> {
      public ObservableSortedCollection(ObservableCollection<T> source) : this(source, Comparer<T>.Default) { }

      public ObservableSortedCollection(ObservableCollection<T> source, IComparer<T> comparer) {
         _comparer = comparer;
         foreach (var item in source) _insert_sorted(item);
         source.CollectionChanged += source_CollectionChanged;
      }

      private IComparer<T> _comparer;
      public IComparer<T> comparer { get { return _comparer; } set { _comparer = value; sort(); } }

      protected void sort() { this.sync_with(this.OrderBy(t => t, _comparer)); }

      private void source_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e) {
         switch (e.Action) {
            case NotifyCollectionChangedAction.Add: foreach (T item in e.NewItems) _insert_sorted(item); return;
            case NotifyCollectionChangedAction.Remove: foreach (T item in e.OldItems) Remove(item); return;
            default:
               if (e.OldItems != null) foreach (T item in e.OldItems) Remove(item);
               if (e.NewItems != null) foreach (T item in e.NewItems) _insert_sorted(item);
               return;
         }
      }

      private void _insert_sorted(T item) { _insert_sorted(item, 0, Count); }
      // Takes advantage of the fact that all elements are assumed to be sorted, recursively hones in on the insert index.
      private void _insert_sorted(T item, int index_range_begin, int index_range_end) {
         if (index_range_begin == index_range_end) { Insert(index_range_begin, item); return; }
         if (index_range_end == (index_range_begin + 1)) {
            if (_comparer.Compare(item, this[index_range_begin]) <= 0) Insert(index_range_begin, item);
            else Insert(index_range_end, item);
            return;
         }
         int middle_index = index_range_begin + (int)((index_range_end - index_range_begin) * 0.5);
         int comp = _comparer.Compare(item, this[middle_index]);
         if (comp > 0) _insert_sorted(item, middle_index + 1, index_range_end);
         else if (comp < 0) {
            if (_comparer.Compare(item, this[middle_index - 1]) >= 0) { Insert(middle_index, item); return; }
            _insert_sorted(item, index_range_begin, middle_index - 1);
         }
         else { Insert(middle_index, item); return; }
      }
   }
}