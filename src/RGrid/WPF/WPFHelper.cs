using Disposable.Extensions;
using Disposable.Extensions.Utilities;
using EqualityComparer.Extensions;
using RGrid.Utility;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace RGrid.WPF {
   static class WPFHelper {
      public delegate void PropertyChangedCallback<TValue, TOwner>(TOwner owner, TValue old_value, TValue new_value);

      public static DependencyProperty create_dp<TValue, TOwner>(string name, TValue default_value = default(TValue)) =>
         DependencyProperty.Register(name, typeof(TValue), typeof(TOwner), new PropertyMetadata(default_value));

      public static DependencyProperty create_dp<TValue, TOwner>(string name, Action<TOwner, TValue> callback, TValue default_value = default(TValue)) =>
         DependencyProperty.Register(name, typeof(TValue), typeof(TOwner), new PropertyMetadata(default_value, prop_changed_callback(callback)));

      public static DependencyProperty create_dp<TValue, TOwner>(string name, Action<TOwner, TValue> callback, Func<TOwner, TValue, TValue> coerce_value, TValue default_value = default(TValue)) =>
         DependencyProperty.Register(name, typeof(TValue), typeof(TOwner), new PropertyMetadata(default_value, callback == null ? null : prop_changed_callback(callback), coerce_value_callback(coerce_value)));

      public static DependencyProperty create_dp<TValue, TOwner>(string name, FrameworkPropertyMetadataOptions options, TValue default_value = default(TValue)) =>
         DependencyProperty.Register(name, typeof(TValue), typeof(TOwner), new FrameworkPropertyMetadata(default_value, options));

      public static DependencyProperty create_dp<TValue, TOwner>(string name, FrameworkPropertyMetadataOptions options, Action<TOwner, TValue> callback, TValue default_value = default(TValue)) =>
         DependencyProperty.Register(name, typeof(TValue), typeof(TOwner), new FrameworkPropertyMetadata(default_value, options, prop_changed_callback(callback)));

      public static DependencyProperty create_dp<TValue, TOwner>(string name, FrameworkPropertyMetadataOptions options, Func<TOwner, TValue, TValue> coerce_value, TValue default_value = default(TValue)) =>
          DependencyProperty.Register(name, typeof(TValue), typeof(TOwner), new FrameworkPropertyMetadata(default_value, options, null, coerce_value_callback(coerce_value)));

      public static DependencyProperty create_dp<TValue, TOwner>(string name, FrameworkPropertyMetadataOptions options, Action<TOwner, TValue> callback, Func<TOwner, TValue, TValue> coerce_value, TValue default_value = default(TValue)) =>
         DependencyProperty.Register(name, typeof(TValue), typeof(TOwner), new FrameworkPropertyMetadata(default_value, options, prop_changed_callback(callback), coerce_value_callback(coerce_value)));

      public static DependencyProperty create_dp<TValue, TOwner>(string name, PropertyChangedCallback<TValue, TOwner> callback, TValue default_value = default(TValue)) =>
         DependencyProperty.Register(name, typeof(TValue), typeof(TOwner), new PropertyMetadata(default_value, prop_changed_callback(callback)));

      public static DependencyPropertyKey create_ro_dp<TValue, TOwner>(string name, TValue default_value = default(TValue)) =>
         DependencyProperty.RegisterReadOnly(name, typeof(TValue), typeof(TOwner), new PropertyMetadata(default_value));

      public static DependencyPropertyKey create_ro_dp<TValue, TOwner>(string name, Action<TOwner, TValue> callback, TValue default_value = default(TValue)) =>
         DependencyProperty.RegisterReadOnly(name, typeof(TValue), typeof(TOwner), new PropertyMetadata(default_value, prop_changed_callback(callback)));

      public static DependencyPropertyKey create_ro_dp<TValue, TOwner>(string name, Action<TOwner, TValue> callback, Func<TOwner, TValue, TValue> coerce_value, TValue default_value = default(TValue)) =>
         DependencyProperty.RegisterReadOnly(name, typeof(TValue), typeof(TOwner), new PropertyMetadata(default_value, prop_changed_callback(callback), coerce_value_callback(coerce_value)));

      public static void override_metadata<T>(DependencyProperty default_style_key_prop) =>
         default_style_key_prop.OverrideMetadata(typeof(T), new FrameworkPropertyMetadata(typeof(T)));

      public static void override_metadata<TValue, TOwner>(DependencyProperty property, FrameworkPropertyMetadataOptions flags, TValue default_value = default(TValue)) =>
         property.OverrideMetadata(typeof(TOwner), new FrameworkPropertyMetadata(default_value, flags));

      public static void override_metadata<TValue, TOwner>(DependencyProperty property, FrameworkPropertyMetadataOptions flags, Action<TOwner, TValue> callback, TValue default_value = default(TValue)) =>
         property.OverrideMetadata(typeof(TOwner), new FrameworkPropertyMetadata(default_value, flags, prop_changed_callback(callback)));

      public static NotifyCollectionChangedEventHandler create_collection_changed_cb<T>(Action<T> on_added, Action<T> on_removed, Action<object> on_reset = null) =>
         (s, e) => {
            switch (e.Action) {
               case NotifyCollectionChangedAction.Add:
                  foreach (T item in e.NewItems)
                     on_added(item);
                  break;
               case NotifyCollectionChangedAction.Remove:
                  foreach (T item in e.OldItems)
                     on_removed(item);
                  break;
               case NotifyCollectionChangedAction.Replace:
                  foreach (T item in e.OldItems)
                     on_removed(item);
                  foreach (T item in e.NewItems)
                     on_added(item);
                  break;
               case NotifyCollectionChangedAction.Reset:
                  on_reset?.Invoke(s);
                  break;
            }
         };

      public static NotifyCollectionChangedEventHandler create_collection_changed_cb(
         Action<IList, int> on_items_added = null, Action<IList, int> on_items_removed = null, Action<IList, int, IList, int> on_items_moved = null, Action<IList, int, IList, int> on_items_replaced = null, Action on_reset = null) =>
         (s, e) => {
            switch (e.Action) {
               case NotifyCollectionChangedAction.Add: on_items_added?.Invoke(e.NewItems, e.NewStartingIndex); break;
               case NotifyCollectionChangedAction.Remove: on_items_removed?.Invoke(e.OldItems, e.OldStartingIndex); break;
               case NotifyCollectionChangedAction.Move: on_items_moved?.Invoke(e.OldItems, e.OldStartingIndex, e.NewItems, e.NewStartingIndex); break;
               case NotifyCollectionChangedAction.Replace: on_items_replaced?.Invoke(e.OldItems, e.OldStartingIndex, e.NewItems, e.NewStartingIndex); break;
               case NotifyCollectionChangedAction.Reset: on_reset?.Invoke(); break;
            }
         };

      public static void manage_collection_change(DependencyPropertyChangedEventArgs e, NotifyCollectionChangedEventHandler handler) {
         if (e.OldValue is INotifyCollectionChanged old_ncc) {
            old_ncc.CollectionChanged -= handler;
            handler(e.OldValue, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
         }
         if (e.NewValue is INotifyCollectionChanged new_ncc) {
            new_ncc.CollectionChanged += handler;
            if (e.NewValue is IEnumerable items)
               foreach (var item in items)
                  handler(e.NewValue, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add, item));
         }
      }

      public static void manage_collection_change(DependencyPropertyChangedEventArgs e, NotifyCollectionChangedEventHandler handler, Action<INotifyCollectionChanged> on_old, Action<INotifyCollectionChanged> on_new) =>
        manage_collection_change(e.OldValue as INotifyCollectionChanged, e.NewValue as INotifyCollectionChanged, handler, on_old, on_new);

      public static void manage_collection_change(INotifyCollectionChanged old_val, INotifyCollectionChanged new_val, NotifyCollectionChangedEventHandler handler) =>
         manage_collection_change(old_val, new_val, handler, null, null);

      public static void manage_collection_change(INotifyCollectionChanged old_val, INotifyCollectionChanged new_val, NotifyCollectionChangedEventHandler handler, Action<INotifyCollectionChanged> on_old, Action<INotifyCollectionChanged> on_new) {
         if (old_val != null) {
            old_val.CollectionChanged -= handler;
            on_old?.Invoke(old_val);
         }
         if (new_val != null) {
            new_val.CollectionChanged += handler;
            on_new?.Invoke(new_val);
         }
      }

      public static void select_first_item<T, TSource>(TSource source, Action<T> set_selected_item) where TSource : IList<T>, INotifyCollectionChanged =>
         select_first_item(source, new Func<T, bool>(item => {
            set_selected_item(item);
            return true;
         }));

      public static void select_first_item<T, TSource>(TSource source, Func<T, bool> set_selected_item) where TSource : IList<T>, INotifyCollectionChanged {
         foreach (var item in source)
            if (set_selected_item(item)) return;
         NotifyCollectionChangedEventHandler source_changed = null;
         source.CollectionChanged += source_changed = (s, e) => {
            switch (e.Action) {
               case NotifyCollectionChangedAction.Add:
                  foreach (T item in source)
                     if (set_selected_item(item))
                        source.CollectionChanged -= source_changed;
                  break;
            }
         };
      }

      public static RowDefinition get_element_row(System.Windows.Controls.Grid grid, FrameworkElement element) {
         int index = System.Windows.Controls.Grid.GetRow(element);
         var rows = grid.RowDefinitions;
         if (index < 0 || index >= rows.Count) throw new ArgumentOutOfRangeException(nameof(index), $"Element's Grid.Row is out of range ({index})");
         return rows[index];
      }

      public static IDisposable add_input_bindings_to_parent_on_loaded<TParent>(FrameworkElement element, params InputBinding[] input_bindings) where TParent : FrameworkElement {
         RoutedEventHandler on_loaded = null;
         bool has_been_added = false;
         TParent parent = null;
         on_loaded = (s, e) => {
            if (has_been_added = try_add_input_binding_to_parent(element, out parent, input_bindings))
               element.Loaded -= on_loaded;
         };
         element.Loaded += on_loaded;
         return DisposableFactory.Create(() => {
            if (has_been_added) {
               foreach (var ib in input_bindings) parent.InputBindings.Remove(ib);
            } else on_loaded -= on_loaded;
         });
      }

      public static bool try_add_input_binding_to_parent<TParent>(FrameworkElement element, out TParent parent, params InputBinding[] input_bindings) where TParent : FrameworkElement {
         parent = element.find_ancestor_of_type<TParent>();
         if (parent == null) return false;
         foreach (var ib in input_bindings) parent.InputBindings.Add(ib);
         return true;
      }

      public static IDisposable add_key_down_handler_to_parent_on_loaded<TParent>(FrameworkElement element, KeyEventHandler handler) where TParent : FrameworkElement {
         RoutedEventHandler on_loaded = null;
         bool has_been_added = false;
         TParent parent = null;
         on_loaded = (s, e) => {
            if (has_been_added = try_add_key_down_handler_to_parent(element, out parent, handler))
               element.Loaded -= on_loaded;
         };
         element.Loaded += on_loaded;
         return DisposableFactory.Create(() => {
            if (has_been_added) {
               parent.KeyDown -= handler;
            } else on_loaded -= on_loaded;
         });
      }

      public static bool try_add_key_down_handler_to_parent<TParent>(FrameworkElement element, out TParent parent, KeyEventHandler handler) where TParent : FrameworkElement {
         parent = element.find_ancestor_of_type<TParent>();
         if (parent == null) return false;
         parent.KeyDown += handler;
         return true;
      }

      public static void offset_row_star_height(this System.Windows.Controls.Grid grid, int row, double offset) {
         RowDefinition row_def = grid.RowDefinitions[row];
         GridLength row_height = row_def.Height;
         if (!row_height.IsStar) throw new InvalidOperationException("Row must have star height.");
         double pixel_per_star = row_def.ActualHeight / row_def.Height.Value;
         row_def.Height = new GridLength(row_def.Height.Value + offset / pixel_per_star, GridUnitType.Star);
         var other_star_rows = grid.RowDefinitions.Where((r, i) => i != row && r.Height.IsStar).ToList();
         double marginal_adjustment = offset / (pixel_per_star * other_star_rows.Count);
         foreach (var r in other_star_rows)
            r.Height = new GridLength(r.Height.Value - marginal_adjustment, GridUnitType.Star);
      }

      public static IDisposable listen_for_clear_focus_requests(FrameworkElement element) {
         ExceptionAssert.Argument.NotNull(element, nameof(element));
         IDisposable hook = null;
         set_hook(element.DataContext);
         element.DataContextChanged += on_data_context_changed;
         return DisposableFactory.Create(() => {
            element.DataContextChanged -= on_data_context_changed;
            hook?.Dispose();
         });

         void on_data_context_changed(object sender, DependencyPropertyChangedEventArgs e) =>
            set_hook(e.NewValue);

         void on_request((bool clear_focus, bool clear_keyboard_focus) args) {
            if (args.clear_focus) {
               var d = FocusManager.GetFocusScope(element);
               if (d != null)
                  FocusManager.SetFocusedElement(d, null);
            }
            if (args.clear_keyboard_focus)
               Keyboard.ClearFocus();
         }

         void set_hook(object data_context) {
            DisposableUtils.Dispose(ref hook);
            if (data_context is IRequestClearFocus rcf)
               hook = rcf.subscribe(on_request);
         }
      }

      public static void set_if_changed<T>(DependencyObject d, DependencyProperty property, T new_value) =>
         set_if_changed(d, property, new_value, EqualityComparer<T>.Default);

      public static void set_if_changed<T>(DependencyObject d, DependencyProperty property, T new_value, IEqualityComparer<T> comparer) {
         object curr = d.GetValue(property);
         if (!(curr is T curr_t && comparer.Equals(curr_t, new_value)))
            d.SetValue(property, new_value);
      }

      public static void set_current_if_changed<T>(DependencyObject d, DependencyProperty property, T new_value) =>
         set_current_if_changed(d, property, new_value, EqualityComparer<T>.Default);

      public static void set_current_if_changed<T>(DependencyObject d, DependencyProperty property, T new_value, IEqualityComparer<T> comparer) {
         object curr = d.GetValue(property);
         if (!comparer.ToNonGeneric().Equals(d.GetValue(property), new_value))
            d.SetCurrentValue(property, new_value);
      }

      public static PropertyChangedCallback prop_changed_callback<TValue, TOwner>(PropertyChangedCallback<TValue, TOwner> callback) =>
         (d, e) => callback(ConvertUtils.try_convert<TOwner>(d), ConvertUtils.try_convert<TValue>(e.OldValue), ConvertUtils.try_convert<TValue>(e.NewValue));

      public static PropertyChangedCallback prop_changed_callback<TValue, TOwner>(Action<TOwner, TValue> callback) =>
         (d, e) => callback(ConvertUtils.try_convert<TOwner>(d), ConvertUtils.try_convert<TValue>(e.NewValue));

      public static CoerceValueCallback coerce_value_callback<TValue, TOwner>(Func<TOwner, TValue, TValue> coerce_value) =>
         (d, obj) => coerce_value(ConvertUtils.try_convert<TOwner>(d), ConvertUtils.try_convert<TValue>(obj));

      public static double control_base_width(Thickness border_thickness, Thickness padding) =>
         border_thickness.Left + padding.Left + padding.Right + border_thickness.Right;

      public static double control_base_height(Thickness border_thickness, Thickness padding) =>
         border_thickness.Top + padding.Top + padding.Bottom + border_thickness.Bottom;
   }
}
