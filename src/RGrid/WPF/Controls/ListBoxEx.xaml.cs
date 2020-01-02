using Disposable.Extensions;
using RGrid.Utility;
using RGrid.WPF;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;

namespace RGrid.Controls {
	class ListBoxEx : ListBox {
		static ListBoxEx() =>
			DefaultStyleKeyProperty.OverrideMetadata(typeof(ListBoxEx), new FrameworkPropertyMetadata(typeof(ListBoxEx)));

		public ListBoxEx() {
			ItemContainerGenerator.StatusChanged += (s, e) => { _update_item_containers(); _update_is_all_selected(); };
			SetCurrentValue(SelectionProperty, new ObservableCollectionEx<object>());
		}

		protected override void OnItemsSourceChanged(IEnumerable oldValue, IEnumerable newValue) {
			_assert_items_source_can_change();
			base.OnItemsSourceChanged(oldValue, newValue);
			var ov = oldValue == null ? Enumerable.Empty<object>() : oldValue.Cast<object>();
			var nv = newValue == null ? Enumerable.Empty<object>() : newValue.Cast<object>();
			var selected_items = Selection;
			using (_selection_change()) {
				foreach (var item in ov.Except(nv).Where(selected_items.Contains)) selected_items.Remove(item);
				_update_item_containers();
				_update_is_all_selected();
			}
			var old_ncc = ov as INotifyCollectionChanged;
			if (old_ncc != null) old_ncc.CollectionChanged -= ItemsSource_CollectionChanged;
			var new_ncc = nv as INotifyCollectionChanged;
			if (new_ncc != null) new_ncc.CollectionChanged += ItemsSource_CollectionChanged;
		}

		void ItemsSource_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e) {
			_assert_items_source_can_change();
			using (_selection_change()) {
				switch (e.Action) {
					case NotifyCollectionChangedAction.Remove: {
							var selected_items = Selection;
							foreach (var item in e.OldItems) selected_items.Remove(item);
							break;
						}
					case NotifyCollectionChangedAction.Replace: {
							var selected_items = Selection;
							foreach (var item in e.OldItems) if (!e.NewItems.Contains(item)) selected_items.Remove(item);
							break;
						}
					case NotifyCollectionChangedAction.Reset: Selection.Clear(); break;
				}
				_update_is_all_selected();
			}
		}

		void _assert_items_source_can_change() { if (_selection_changing) throw new InvalidOperationException("ItemsSource cannot change while selection is changing."); }

		#region Selection Property

		public static readonly DependencyProperty SelectionProperty = DependencyProperty.Register("Selection", typeof(IObservableCollectionEx), typeof(ListBoxEx),
			new FrameworkPropertyMetadata(null, OnSelectionChanged, CoerceSelection));

		static void OnSelectionChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) {
			var target = (ListBoxEx)d;
			if (target._selection_changing) throw new InvalidOperationException("Selection cannot be re-assigned while selection is changing.");
			var osi = e.OldValue as IObservableCollectionEx;
			if (osi != null) osi.CollectionChanged -= target.SelectionCollectionChanged;
			var nsi = e.NewValue as IObservableCollectionEx;
			if (nsi != null) nsi.CollectionChanged += target.SelectionCollectionChanged;
			using (target._selection_change()) {
				target._update_item_containers();
				target._update_is_all_selected();
			}
		}

		static object CoerceSelection(DependencyObject d, object baseValue) {
			if (baseValue is IObservableCollectionEx) return baseValue;
			var target = (ListBoxEx)d;
			if (target.ItemsSource != null) {
				Type gent_def = target.ItemsSource.GetType().GetInterfaces().FirstOrDefault(it => it.GetGenericTypeDefinition() == typeof(IEnumerable<>));
				if (gent_def != null) {
					Type gent_arg = gent_def.GetGenericArguments().First();
					return Activator.CreateInstance(typeof(ObservableCollectionEx<>).MakeGenericType(gent_arg));
				}
			}
			return new ObservableCollectionEx<object>();
		}

		void SelectionCollectionChanged(object sender, NotifyCollectionChangedEventArgs e) {
			if (_selection_changing) return;
			using (_selection_change()) {
				switch (e.Action) {
					case NotifyCollectionChangedAction.Add: foreach (var item in e.NewItems) _set_is_selected(item, true); break;
					case NotifyCollectionChangedAction.Remove: foreach (var item in e.OldItems) _set_is_selected(item, false); break;
					case NotifyCollectionChangedAction.Replace:
						if (e.OldItems != null) foreach (var item in e.OldItems) _set_is_selected(item, false);
						if (e.NewItems != null) foreach (var item in e.NewItems) _set_is_selected(item, true); break;
					case NotifyCollectionChangedAction.Reset: foreach (var item in ItemsSource) _set_is_selected(item, false); break;
				}
				_update_is_all_selected();
			}
		}

		public IObservableCollectionEx Selection { get { return GetValue(SelectionProperty) as IObservableCollectionEx; } set { SetValue(SelectionProperty, value); } }

		#endregion

		#region IsAllSelected Property

		public static readonly DependencyProperty IsAllSelectedProperty = DependencyProperty.Register("IsAllSelected", typeof(bool?), typeof(ListBoxEx),
			new FrameworkPropertyMetadata(new bool?(false), OnIsAllSelectedChanged));

		static void OnIsAllSelectedChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) {
			var target = (ListBoxEx)d;
			if (target._selection_changing) return;
			bool? ias = e.NewValue as bool?;
			if (!ias.HasValue) return;
			using (target._selection_change()) {
				var selected_items = target.Selection;
				if (ias.Value) {
					foreach (var item in target.ItemsSource) if (!selected_items.Contains(item)) selected_items.Add(item);
				} else selected_items.Clear();
				target._update_item_containers();
			}
		}

		public bool? IsAllSelected {
			get {
				// Because Nullable<bool> unboxing is very slow (uses reflection) first we cast to bool
				object value = GetValue(IsAllSelectedProperty);
				return value == null ? new bool?() : new bool?((bool)value);
			}
			set { SetValue(IsAllSelectedProperty, value.HasValue ? BooleanBoxes.Box(value.Value) : null); }
		}


		#endregion

		bool _selection_changing;

		IDisposable _selection_change() {
			if (_selection_changing) throw new InvalidOperationException("Selection-change is already underway.");
			_selection_changing = true;
			return DisposableFactory.Create(() => _selection_changing = false);
		}

		void _update_item_containers() {
			if (ItemContainerGenerator.Status != GeneratorStatus.ContainersGenerated) return;
			var items = ItemsSource ?? Items;
			if (items == null) return;
         SelectedItems.Cast<object>().difference(Selection.Cast<object>(), out IEnumerable<object> added, out IEnumerable<object> removed);
         foreach (var item in removed) {
            SelectedItems.Remove(item);
         }
         foreach (var item in added) {
            SelectedItems.Add(item);
         }
		}

		void _update_is_all_selected() {
			var items = ItemsSource ?? Items;
			if (items == null) return;
			int n_total = items.Cast<object>().Count();
			if (n_total == 0) IsAllSelected = null;
			else {
				int n_selected = Selection.Count;
				IsAllSelected = n_selected == n_total ? new bool?(true) : n_selected == 0 ? new bool?(false) : null;
			}
		}

		void _set_is_selected(object item, bool is_selected) {
         var view_selection = SelectedItems;
         if (is_selected) {
            if (!view_selection.Contains(item)) view_selection.Add(item);
         } else view_selection.Remove(item);
		}

		void item_Selected(object sender, RoutedEventArgs e) {
			if (_selection_changing) return;
			var item = ((ListBoxItem)sender).DataContext;
			var selected_items = Selection;
			if (!selected_items.Contains(item)) {
				using (_selection_change()) {
					selected_items.Add(item);
					_update_is_all_selected();
				}
			}
		}

      void item_Unselected(object sender, RoutedEventArgs e) {
         if (!_selection_changing && sender is ListBoxItem container) {
            // the [ListBoxItem] sender may be "Disconnected" https://stackoverflow.com/questions/14282894/wpf-listbox-virtualization-creates-disconnecteditems/14358688
            var item = ItemContainerGenerator.ItemFromContainer(container);
            if (item != DependencyProperty.UnsetValue) {
               var selected_items = Selection;
               if (selected_items.Contains(item)) {
                  using (_selection_change()) {
                     selected_items.Remove(item);
                     _update_is_all_selected();
                  }
               }
            }
         }
      }

		void item_DataContextChanged(object sender, DependencyPropertyChangedEventArgs e) {
			using (_selection_change()) ((ListBoxItem)sender).IsSelected = Selection.Contains(e.NewValue);
		}

		protected override DependencyObject GetContainerForItemOverride() {
			var container = new ListBoxItem();
			_initialize_item_container(container);
			return container;
		}

		protected override bool IsItemItsOwnContainerOverride(object item) {
			var container = item as ListBoxItem;
			if (container == null) return false;
			_initialize_item_container(container);
			return true;
		}

		void _initialize_item_container(ListBoxItem container) {
			container.Selected += item_Selected;
			container.Unselected += item_Unselected;
			container.DataContextChanged += item_DataContextChanged;
		}
	}

	//[TestFixture]
	//class ListBoxExUnitTest
	//{
	//	readonly object[] _items = { "alice", "bob", "chuck", "dave" }, _alt_items = { "shamir", "monique", "pierre", "lakeesha", "kilroy" };

	//	[Test, Apartment(System.Threading.ApartmentState.STA)]
	//	public void state_maintained_on_is_all_selected_changed() {
	//		ListBoxEx lb = new ListBoxEx();
	//		_set_items_source(lb, _items);
	//		_assert_items_vvm_synchronized(lb, false);
	//		lb.IsAllSelected = true;
	//		_assert_items_vvm_synchronized(lb, true);
	//		lb.IsAllSelected = false;
	//		_assert_items_vvm_synchronized(lb, false);
	//	}

	//	[Test, Apartment(System.Threading.ApartmentState.STA)]
	//	public void state_maintained_on_selected_items_collection_changed() {
	//		ListBoxEx lb = new ListBoxEx();
	//		_set_items_source(lb, _items);
	//		lb.Selection.AddRange(lb.ItemsSource);
	//		_assert_items_vvm_synchronized(lb, true);
	//		lb.Selection.RemoveAt(0);
	//		_assert_items_vvm_synchronized(lb, null);
	//		lb.Selection.Clear();
	//		_assert_items_vvm_synchronized(lb, false);
	//	}

	//	[Test, Apartment(System.Threading.ApartmentState.STA)]
	//	public void state_maintained_on_selected_items_reassigned() {
	//		ListBoxEx lb = new ListBoxEx();
	//		_set_items_source(lb, _items);
	//		lb.Selection = new ObservableCollectionEx<object>();
	//		_assert_items_vvm_synchronized(lb, false);
	//		lb.Selection = new ObservableCollectionEx<object>(_items.Skip(1));
	//		_assert_items_vvm_synchronized(lb, null);
	//		lb.Selection = new ObservableCollectionEx<object>(_items);
	//		_assert_items_vvm_synchronized(lb, true);
	//	}

	//	[Test, Apartment(System.Threading.ApartmentState.STA)]
	//	public void state_maintained_on_items_source_collection_changed() {
	//		ListBoxEx lb = new ListBoxEx();
	//		var items = new System.Collections.ObjectModel.ObservableCollection<object>(_items);
	//		_set_items_source(lb, items);
	//		lb.IsAllSelected = true;
	//		_assert_items_vvm_synchronized(lb, true);
	//		items.Clear();
	//		_assert_items_vvm_synchronized(lb, null);
	//		foreach (var ai in _alt_items) items.Add(ai);
	//		_force_item_realization(lb);
	//		_assert_items_vvm_synchronized(lb, false);
	//		lb.IsAllSelected = true;
	//		_assert_items_vvm_synchronized(lb, true);
	//		lb.Selection.RemoveAt(0);
	//		_assert_items_vvm_synchronized(lb, null);
	//		items.RemoveAt(0);
	//		_assert_items_vvm_synchronized(lb, true);
	//		items.RemoveAt(0);
	//		_assert_items_vvm_synchronized(lb, true);
	//	}

	//	[Test, Apartment(System.Threading.ApartmentState.STA)]
	//	public void state_maintained_on_items_source_reassigned() {
	//		ListBoxEx lb = new ListBoxEx();
	//		_set_items_source(lb, _items);
	//		_assert_items_vvm_synchronized(lb, false);
	//		lb.IsAllSelected = true;
	//		_assert_items_vvm_synchronized(lb, true);
	//		_set_items_source(lb, _alt_items);
	//		_assert_items_vvm_synchronized(lb, false);
	//		lb.IsAllSelected = true;
	//		_assert_items_vvm_synchronized(lb, true);
	//		lb.Selection.RemoveAt(0);
	//		_assert_items_vvm_synchronized(lb, null);
	//		_set_items_source(lb, lb.Selection.Cast<object>().ToList());
	//		_assert_items_vvm_synchronized(lb, true);
	//		_set_items_source(lb, lb.Selection.Cast<object>().Skip(1).ToList());
	//		_assert_items_vvm_synchronized(lb, true);
	//	}

	//	[Test, Apartment(System.Threading.ApartmentState.STA)]
	//	public void state_maintained_on_item_view_is_selected_changed() {
	//		ListBoxEx lb = new ListBoxEx();
	//		_set_items_source(lb, _items);
	//		var item_view = lb.ItemContainerGenerator.ContainerFromIndex(0) as ListBoxItem;
	//		Assert.NotNull(item_view);
	//		Assert.False(item_view.IsSelected);
	//		Assert.False(lb.IsAllSelected.Value);
	//		item_view.IsSelected = true;
	//		_assert_items_vvm_synchronized(lb, null);
	//		Assert.AreEqual(1, lb.Selection.Count);
	//		Assert.AreEqual(lb.Selection[0], item_view.DataContext);
	//		item_view.IsSelected = false;
	//		_assert_items_vvm_synchronized(lb, false);
	//	}

	//	/// <summary>
	//	/// Asserts that IsAllSelected, Selection, and [ListBoxItem] Item views are all synchorinzed with one another.
	//	/// </summary>
	//	void _assert_items_vvm_synchronized(ListBoxEx lb, bool? expected_is_all_selected) {
	//		bool? is_all_selected = lb.IsAllSelected;
	//		var selected_items_set = new HashSet<object>(lb.Selection.Cast<object>());
	//		var items_source_set = new HashSet<object>(lb.ItemsSource.Cast<object>());
	//		Assert.AreEqual(selected_items_set.Count, lb.Selection.Count);
	//		Assert.AreEqual(items_source_set.Count, lb.ItemsSource.Count());
	//		Assert.AreEqual(expected_is_all_selected.HasValue, is_all_selected.HasValue);
	//		if (is_all_selected.HasValue) {
	//			Assert.AreEqual(expected_is_all_selected.Value, is_all_selected.Value);
	//			if (is_all_selected.Value) Assert.True(selected_items_set.SetEquals(items_source_set));
	//			else Assert.AreEqual(0, selected_items_set.Count);
	//		} else {
	//			if (items_source_set.Count == 0) Assert.AreEqual(0, selected_items_set.Count);
	//			else {
	//				Assert.Greater(selected_items_set.Count, 0);
	//				Assert.Less(selected_items_set.Count, items_source_set.Count);
	//			}
	//		}
	//		foreach (var item in items_source_set) {
	//			var item_view = lb.ItemContainerGenerator.ContainerFromItem(item) as ListBoxItem;
	//			Assert.NotNull(item_view);
	//			Assert.AreEqual(item_view.IsSelected, selected_items_set.Contains(item));
	//		}
	//	}

	//	void _set_items_source(ListBoxEx lb, IEnumerable items_source) {
	//		lb.ItemsSource = items_source;
	//		_force_item_realization(lb);
	//	}

	//	void _force_item_realization(ListBoxEx lb) {
	//		int count = lb.ItemsSource.Count();
	//		IItemContainerGenerator generator = lb.ItemContainerGenerator;
	//		using (generator.StartAt(generator.GeneratorPositionFromIndex(0), System.Windows.Controls.Primitives.GeneratorDirection.Forward)) {
	//			for (int i = 0; i < count; i++) {
	//				bool is_new;
	//				var container = generator.GenerateNext(out is_new);
	//				if (is_new) generator.PrepareItemContainer(container);
	//			}
	//		}
	//	}
	//}
}
