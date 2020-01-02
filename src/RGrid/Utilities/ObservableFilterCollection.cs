using Collections.Sync.Utils;
using Disposable.Extensions;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;

namespace RGrid.Utility {
   static class ObservableFilterCollection {
      // Originally designed for RGrid.Columns, so they may be filtered by column.is_visible
      // Because RGrid.Columns are directly linked to visual-items, no column can exist more than once in the collection at a time.
      // The normal sync_with method cannot be used in this case, bc it may insert an item into a new index before removing it from the old one.
      // Note: NOT intended for use with LARGE COLLECTIONS.
      public static IDisposable connect_never_duplicate<T>(ObservableCollection<T> source_collection, ObservableCollection<T> filtered_collection, Func<T, bool> filter, params string[] live_filter_props) {
         reset();
         return source_collection.Subscribe(on_source_CollectionChanged);
         //
         void on_source_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e) {
            // Note: add/remove_items calls here could be a bit more optimized by taking advantage of the fact that we already know the source_collection index of the new/old items.
            // However, seems like small potatoes; this method is not intended for use with large collections anyways.
            switch (e.Action) {
               case NotifyCollectionChangedAction.Add: add_items(e.NewItems); break;
               case NotifyCollectionChangedAction.Remove: remove_items(e.OldItems); break;
               case NotifyCollectionChangedAction.Replace:
               case NotifyCollectionChangedAction.Move:
                  remove_items(e.OldItems);
                  add_items(e.NewItems);
                  break;
               case NotifyCollectionChangedAction.Reset: reset(); break;

            }
         }

         void add_items(IList items) {
            foreach (T value in items) {
               if (filter(value)) {
                  insert_item(value);
               }
               if (!live_filter_props.is_null_or_empty() && value is INotifyPropertyChanged npc) {
                  npc.PropertyChanged += on_item_PropertyChanged;
               }
            }
         }

         void remove_items(IList items) {
            foreach(T value in items) {
               filtered_collection.Remove(value);
               if (!live_filter_props.is_null_or_empty() && value is INotifyPropertyChanged npc) {
                  npc.PropertyChanged -= on_item_PropertyChanged;
               }
            }
         }

         void on_item_PropertyChanged(object sender, PropertyChangedEventArgs e) {
            if (live_filter_props.Contains(e.PropertyName)) {
               T value = (T)sender;
               if (!filter(value)) {
                  filtered_collection.Remove(value);
               } else if (!filtered_collection.Contains(value)) {
                  insert_item(value);
               }
            }
         }

         void insert_item(T value) {
            int index = get_insertion_index(value);
            filtered_collection.Insert(index, value);
         }

         int get_insertion_index(T value) {
            int fi = 0;
            for (int si = 0; si < source_collection.Count; si++) {
               if (si == filtered_collection.Count) {
                  return si;
               } else if (source_collection[si].Equals(value)) {
                  return fi;
               } else if (filtered_collection[fi].Equals(source_collection[si]))
                  ++fi;
            }
            return fi;
         }

         void reset() {
            if (!live_filter_props.is_null_or_empty()) {
               foreach (var value in source_collection.OfType<INotifyPropertyChanged>())
                  value.PropertyChanged += on_item_PropertyChanged;
            }
            CollectionHelper.sync_with_never_duplicate(filtered_collection, source_collection.Where(filter));
         }
      }
   }
   /// <summary>
   /// Filters the elements of an ObservableCollection&lt;T&gt;
   /// </summary>
   class ObservableFilterCollection<T> : ObservableCollection<T> {
      readonly ObservableCollection<T> _inner;
      Func<T, bool> _filter;
      HashSet<string> _live_filter_properties_set = new HashSet<string>();
      Dictionary<T, PropertyChangedEventHandler> _property_changed_hooks = new Dictionary<T, PropertyChangedEventHandler>();

      /// <summary>
      /// Initializes a new instance of the ObservableFilterCollection&lt;T&gt;
      /// </summary>
      /// <param name="source">The unfiltered source collection.</param>
      /// <param name="filter">The initial element filter.</param>
      public ObservableFilterCollection(ObservableCollection<T> source, Func<T, bool> filter) {
         _inner = source;
         _filter = filter;
         this.AddRange(source.Where(_filter));
         source.CollectionChanged += _inner_CollectionChanged;
         live_filter_properties.CollectionChanged += _live_filter_properties_CollectionChanged;
      }

      public Func<T, bool> filter {
         get => _filter;
         set {
            _filter = value;
            trigger_filter();
         }
      }

      public ObservableCollection<string> live_filter_properties { get; } = new ObservableCollection<string>();

      public IDisposable defer_refresh() {
         _inner.CollectionChanged -= _inner_CollectionChanged;
         live_filter_properties.CollectionChanged -= _live_filter_properties_CollectionChanged;
         return DisposableFactory.Create(() => {
            trigger_filter();
            _inner.CollectionChanged += _inner_CollectionChanged;
            live_filter_properties.CollectionChanged += _live_filter_properties_CollectionChanged;
         });
      }

      public void trigger_filter() {
         // This is not the most efficient way to do this but it should work without needing more testing
         // Probably won't become a bottleneck as most collections will be small
         this.sync_with(_inner.Where(_filter));
      }

      void _inner_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e) {
         switch (e.Action) {
            case NotifyCollectionChangedAction.Add:
               _hook_items(e.NewItems);
               break;
            case NotifyCollectionChangedAction.Remove:
               _unhook_items(e.OldItems);
               break;
            case NotifyCollectionChangedAction.Replace:
               if (e.OldItems != null)
                  _unhook_items(e.OldItems);
               if (e.NewItems != null)
                  _hook_items(e.NewItems);
               break;
            case NotifyCollectionChangedAction.Reset:
               foreach (var kv in _property_changed_hooks)
                  ((INotifyPropertyChanged)kv.Key).PropertyChanged -= kv.Value;
               _hook_items(_inner);
               break;
         }
         trigger_filter();
      }

      void _live_filter_properties_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e) {
         _live_filter_properties_set = new HashSet<string>(live_filter_properties);
         trigger_filter();
      }

      void _hook_items(IList added_items) {
         foreach (T item in added_items) {
            if (item is INotifyPropertyChanged npc) {
               var hook = _create_prop_changed_hook(item);
               _property_changed_hooks[item] = hook;
               npc.PropertyChanged += hook;
            }
         }
      }

      void _unhook_items(IList removed_items) {
         foreach (T item in removed_items) {
            if (item is INotifyPropertyChanged npc) {
               if (_property_changed_hooks.TryGetValue(item, out PropertyChangedEventHandler hook)) {
                  _property_changed_hooks.Remove(item);
                  npc.PropertyChanged -= hook;
               }
            }
         }
      }

      PropertyChangedEventHandler _create_prop_changed_hook(T item) =>
         new PropertyChangedEventHandler((s, e) => {
            if (_live_filter_properties_set.Contains(e.PropertyName)) {
               if (filter(item)) {
                  if (!Contains(item)) {
                     int index = _get_insert_index(item);
                     if (index >= 0 && index < Count)
                        Insert(index, item);
                     else Add(item);
                  }
               } else Remove(item);
            }
         });

      int _get_insert_index(T item) {
         int i = 0;
         foreach (var inner_item in _inner) {
            if (inner_item.Equals(item))
               break;
            else if (filter(item))
               i++;
         }
         return i;
      }
   }
}