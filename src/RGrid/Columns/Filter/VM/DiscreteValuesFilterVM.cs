using Collections.Sync;
using Disposable.Extensions.Utilities;
using RGrid.Utility;
using RGrid.WPF;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Reactive.Linq;

namespace RGrid.Filters {
   public class SelectableItemVM<T> : ViewModelBase {
      bool _is_selected;

      public SelectableItemVM(T value) =>
         Value = value;

      public bool IsSelected {
         get => _is_selected;
         set => Set(ref _is_selected, value);
      }

      public T Value { get; }
   }

   class MultiSelectModel<T> {
      readonly IComparer<T> _comparer;
      readonly HashSet<T> _desired_selected_items;
      readonly ObservableCollection<T> _items, _selected_items;

      public IStrongReadOnlyObservableCollection<SelectableItemVM<T>> Items { get; }
      public IStrongReadOnlyObservableCollection<T> SelectedItems { get; }

      public void SetSelection(IEnumerable<T> items) {

      }
   }

   public class DiscreteValuesFilterVM<TRow, TItemKey, TItem, TState> : FilterVM<TRow, TItem, TState> {
      readonly Func<TRow, TItem> _get_row_val;
      readonly Func<TState, IObservable<(IEnumerable<TItem> added, IEnumerable<TItem> removed)>> _subscribe_incremental;
      readonly Func<IEnumerable<TItem>, TState> _get_state;
      readonly ObservableCollection<SelectableItemVM<TItem>> _items = new ObservableCollection<SelectableItemVM<TItem>>();
      readonly IComparer<TItem> _comparer;
      readonly HashSet<TItem> _selected_items;

      public DiscreteValuesFilterVM(
         Func<TRow, TItem> get_row_val,
         Func<TState, IObservable<(IEnumerable<TItem> added, IEnumerable<TItem> removed)>> subscribe_incremental,
         Func<IEnumerable<TItem>, TState> get_state,
         IComparer<TItem> comparer) {
         //
         _get_row_val = get_row_val ?? throw new ArgumentNullException(nameof(get_row_val));
         _subscribe_incremental = subscribe_incremental ?? throw new ArgumentNullException(nameof(subscribe_incremental));
         _get_state = get_state ?? throw new ArgumentNullException(nameof(get_state));
         _comparer = comparer ?? throw new ArgumentNullException(nameof(comparer));
         Items = ObservableCollectionFactory.CreateStrongReadOnly(
            ObservableAutoWrapper.CreateReadOnly(_items));
      }

      public IStrongReadOnlyObservableCollection<SelectableItemVM<TItem>> Items { get; }

      public override TState GetState() =>
         _get_state(_selected_items);

      public void UpdateItems(IEnumerable<TItem> add, IEnumerable<TItem> remove) {
      }

      protected override IObservable<Func<TRow, bool>> CreatePredicateSource(TState state) => throw new NotImplementedException();
   }

   public static class DiscreteValuesFilterVM {
      #region Create
      public static IDataGridColumnPersistentFilter<TRow, TValue[]> Create<TRow, TValue>(
         IReadOnlyCollection<TValue> items,
         Func<TRow, TValue> get_row_val,
         string prop_name
      ) => Create(items, get_row_val, prop_name, EqualityComparer<TValue>.Default);

      public static IDataGridColumnPersistentFilter<TRow, TValue[]> Create<TRow, TValue>(
         IReadOnlyCollection<TValue> items,
         Func<TRow, TValue> get_row_val,
         string prop_name,
         IEqualityComparer<TValue> comparer
      ) => new ReadOnlyItemsFilter<TRow, TValue>(items, get_row_val, prop_name, comparer);

      public static IDataGridColumnPersistentFilter<TRow, TValue[]> Create<TRow, TValue>(
         IObservable<IEnumerable<TValue>> items,
         Func<TRow, TValue> get_row_val,
         string prop_name
      ) => Create(items, get_row_val, s => s.ToArray(), s => Observable.Return(s), prop_name, EqualityComparer<TValue>.Default);

      public static IDataGridColumnPersistentFilter<TRow, TState> Create<TRow, TValue, TState>(
         IObservable<IEnumerable<TValue>> items,
         Func<TRow, TValue> get_row_val,
         Func<ISet<TValue>, TState> get_state,
         Func<TState, IObservable<IEnumerable<TValue>>> load_state,
         string prop_name
      ) => Create(items, get_row_val, get_state, load_state, prop_name, EqualityComparer<TValue>.Default);

      public static IDataGridColumnPersistentFilter<TRow, TState> Create<TRow, TValue, TState>(
         IObservable<IEnumerable<TValue>> items,
         Func<TRow, TValue> get_row_val,
         Func<ISet<TValue>, TState> get_state,
         Func<TState, IObservable<IEnumerable<TValue>>> load_state,
         string prop_name,
         IEqualityComparer<TValue> equality_comparer,
         IComparer<TValue> comparer = null
      ) => new DynamicItemsFilter<TRow, TValue, TState>(items, get_row_val, get_state, load_state, prop_name, equality_comparer, comparer);

      public static IDataGridColumnPersistentFilter<TRow, TState> Create<TRow, TValue, TState>(
        IObservable<(IEnumerable<TValue> added, IEnumerable<TValue> removed)> items,
        Func<TRow, TValue> get_row_val,
        Func<ISet<TValue>, TState> get_state,
        Func<TState, IObservable<IEnumerable<TValue>>> load_state,
        string prop_name,
        IEqualityComparer<TValue> equality_comparer,
        IComparer<TValue> comparer = null
     ) => new DynamicItemsFilter<TRow, TValue, TState>(items, get_row_val, get_state, load_state, prop_name, equality_comparer, comparer);
      #endregion

      #region Implementation (private)
      class ReadOnlyItemsFilter<TRow, TValue> : FilterVMBase<TRow, TValue, TValue[]> {
         readonly IEqualityComparer<TValue> _comparer;

         public ReadOnlyItemsFilter(IReadOnlyCollection<TValue> items, Func<TRow, TValue> get_row_val, string prop_name, IEqualityComparer<TValue> comparer)
            : base(get_row_val, prop_name) {
            this.items = items;
            _comparer = comparer;
         }

         public IReadOnlyCollection<TValue> items { get; }
         public ObservableCollectionEx<TValue> selected_items { get; } = new ObservableCollectionEx<TValue>();

         public override TValue[] GetState() => selected_items.ToArray();
         protected override void _clear() {
            _destroy_snapshot();
            selected_items.Clear();
            _close();
            _raise_filter_changed();
         }

         protected override bool _filter(TValue value) =>
            selected_items.Contains(value);

         protected override bool _get_active() =>
            selected_items.Count > 0;

         protected override IObjSnapshot _get_snapshot() =>
            new SelectedItemsSnapshot<TValue>(selected_items, items);

         protected override void _load_state_internal(TValue[] state) {
            if (state == null || state.Length == 0)
               selected_items.Clear();
            else selected_items.sync_with(state.Intersect(items, _comparer));
         }
      }

      class DynamicItemsFilter<TRow, TValue, TState> : FilterVMBase<TRow, TValue, TState>, IDisposable {
         readonly IEqualityComparer<TValue> _equality_comparer;
         readonly Func<ISet<TValue>, TState> _get_state;
         readonly Func<TState, IObservable<IEnumerable<TValue>>> _load_state;
         readonly HashSet<TValue> _items_set, _selected_items_set;
         readonly ObservableCollection<TValue> _unsorted_items;
         bool _override_desired_selection;
         IDisposable _desired_items_sub, _items_sub;

         public DynamicItemsFilter(
            IObservable<IEnumerable<TValue>> items,
            Func<TRow, TValue> get_row_val,
            Func<ISet<TValue>, TState> get_state,
            Func<TState, IObservable<IEnumerable<TValue>>> load_state,
            string prop_name,
            IEqualityComparer<TValue> equality_comparer,
            IComparer<TValue> comparer = null
         ) : this(get_row_val, get_state, load_state, prop_name, equality_comparer, comparer) {
            //
            // REVIS
            //_items_sub = items.OnDispatcher().Subscribe(new_items => {
            //   _items_set.SetItems(new_items.Concat(_selected_items_set));
            //   _unsorted_items.sync_with(_items_set);
            //});
         }
 
         public DynamicItemsFilter(
            IObservable<(IEnumerable<TValue> added, IEnumerable<TValue> removed)> items,
            Func<TRow, TValue> get_row_val,
            Func<ISet<TValue>, TState> get_state,
            Func<TState, IObservable<IEnumerable<TValue>>> load_state,
            string prop_name,
            IEqualityComparer<TValue> equality_comparer,
            IComparer<TValue> comparer = null
         ) : this(get_row_val, get_state, load_state, prop_name, equality_comparer, comparer) {
            //
            // REVIS
            //_items_sub = items.OnDispatcher().Subscribe(update => {
            //   foreach (var v in update.added)
            //      _items_set.Add(v);
            //   foreach (var v in update.removed.Except(_selected_items_set))
            //      _items_set.Remove(v);
            //   _unsorted_items.sync_with(_items_set);
            //});
         }

         DynamicItemsFilter(
            Func<TRow, TValue> get_row_val,
            Func<ISet<TValue>, TState> get_state,
            Func<TState, IObservable<IEnumerable<TValue>>> load_state,
            string prop_name,
            IEqualityComparer<TValue> equality_comparer,
            IComparer<TValue> comparer = null
         ) : base(get_row_val, prop_name) {
            //
            _get_state = get_state;
            _load_state = load_state;
            _equality_comparer = equality_comparer;
            _selected_items_set = new HashSet<TValue>(_equality_comparer);
            _items_set = new HashSet<TValue>(_equality_comparer);
            _unsorted_items = new ObservableCollection<TValue>();
            items = comparer != null ? new ObservableSortedCollection<TValue>(_unsorted_items, comparer) : _unsorted_items;
            selected_items.CollectionChanged += _on_selected_items_CollectionChanged;
         }

         public ObservableCollection<TValue> items { get; }
         public ObservableCollectionEx<TValue> selected_items { get; } = new ObservableCollectionEx<TValue>();

         public void Dispose() {
            DisposableUtils.Dispose(ref _desired_items_sub);
            DisposableUtils.Dispose(ref _items_sub);
            selected_items.CollectionChanged -= _on_selected_items_CollectionChanged;
         }

         public override TState GetState() =>
            _get_state(_selected_items_set);

         protected override bool _filter(TValue value) =>
            _selected_items_set.Contains(value);

         protected override bool _get_active() =>
            selected_items.Count > 0;

         protected override IObjSnapshot _get_snapshot() =>
            new SelectedItemsSnapshot<TValue>(selected_items, items);

         protected override void _load_state_internal(TState state) {
            DisposableUtils.Dispose(ref _desired_items_sub);
            // REVIS
            //_desired_items_sub = _load_state(state).OnDispatcher().Subscribe(_on_desired_item);
         }

         protected override void _apply() {
            // REVIS
            //_selected_items_set.SetItems(selected_items);
            base._apply();
         }

         protected override void _clear() {
            _destroy_snapshot();
            _close();
            selected_items.Clear();
            _selected_items_set.Clear();
            _raise_filter_changed();
         }

         void _on_selected_items_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e) {
            _override_desired_selection = true;
            switch (e.Action) {
               case NotifyCollectionChangedAction.Add:
                  foreach (TValue item in e.NewItems)
                     _selected_items_set.Add(item);
                  break;
               case NotifyCollectionChangedAction.Remove:
                  foreach (TValue item in e.OldItems)
                     _selected_items_set.Remove(item);
                  break;
               case NotifyCollectionChangedAction.Replace:
                  foreach (TValue item in e.OldItems)
                     _selected_items_set.Remove(item);
                  foreach (TValue item in e.NewItems)
                     _selected_items_set.Add(item);
                  break;
               default:
                  _selected_items_set.SetItems(selected_items);
                  break;
            }
         }

         void _on_desired_item(IEnumerable<TValue> dsi) {
            if (!_override_desired_selection && dsi != null) {
               var copy = new HashSet<TValue>(_selected_items_set, _equality_comparer);
               _selected_items_set.SetItems(dsi);
               using (selected_items.SuspendHandler(_on_selected_items_CollectionChanged)) {
                  foreach (var item in _selected_items_set) {
                     if (_items_set.Add(item))
                        _unsorted_items.Add(item);
                  }
                  selected_items.sync_with(_selected_items_set);
               }
               if (!copy.SetEquals(_selected_items_set))
                  _raise_filter_changed();
            }
         }
      }
      #endregion

      [Browsable(false)]
      [EditorBrowsable(EditorBrowsableState.Never)]
      class SelectedItemsSnapshot<TValue> : ViewModelBase.IObjSnapshot {
         readonly IList<TValue> _selected_items;
         readonly IReadOnlyList<TValue> _selected_items_snapshot;
         readonly IEnumerable<TValue> _items;


         public SelectedItemsSnapshot(IList<TValue> selected_items, IEnumerable<TValue> items) {
            _selected_items = selected_items;
            _selected_items_snapshot = selected_items.ToReadOnlyList();
            _items = items;
         }

         public bool revert { get; set; }

         public void Dispose() {
            if (revert)
               _selected_items.sync_with(_selected_items_snapshot.Intersect(_items));
         }
      }
   }
}