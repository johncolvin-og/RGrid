using Collections.Sync.Utils;
using Disposable.Extensions;
using RGrid.Utility;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Reactive.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;

namespace RGrid.WPF {
   static class WPFExtensions {
      public static void bind(this DependencyObject target, DependencyProperty property, IValueConverter converter = null, object source = null, string path = null, params object[] path_parameters) {
         var pp = new PropertyPath(path ?? string.Join(".", Array.ConvertAll(path_parameters, i => $"({i})")));
         for (int i = 0; i < path_parameters.Length; i++)
            pp.PathParameters.Add(path_parameters[i]);
         bind(target, property, pp, converter: converter, source: source);
      }

      public static void bind(this DependencyObject target, DependencyProperty property, PropertyPath path, IValueConverter converter = null, object source = null) =>
         BindingOperations.SetBinding(target, property, new Binding {
            Path = path,
            Source = source,
            Converter = converter
         });

      public static (double x, double y) deconstruct(this Point point) =>
         (point.X, point.Y);

      public static Point minus(this Point point, Point other) =>
         new Point(point.X - other.X, point.Y - other.Y);

      public static Point plus(this Point point, Point other) =>
         new Point(point.X + other.X, point.Y + other.Y);

      public static object get_template_child(this Control control, string name) {
         if (control == null) throw new ArgumentNullException(nameof(control));
         return control?.Template?.FindName(name, control);
      }

      public static T get_template_child<T>(this Control control, string name) {
         object obj = get_template_child(control, name);
         return ConvertUtils.try_convert<T>(obj);
      }

      public static bool try_get_template_child(this Control control, string name, out object result) {
         if (control == null) throw new ArgumentNullException(nameof(control));
         var t = control.Template;
         if (t != null) {
            result = control.Template.FindName(name, control);
            return result != null;
         }
         result = null;
         return false;
      }

      public static bool try_get_template_child<T>(this Control control, string name, out T result) {
         if (try_get_template_child(control, name, out object obj))
            return ConvertUtils.try_convert(obj, out result);
         result = default;
         return false;
      }

      public static object assert_template_child(this Control control, string name) {
         if (!try_get_template_child(control, name, out object result))
            throw new InvalidOperationException($"Template must have a child named '{name}'");
         return result;
      }

      public static T assert_template_child<T>(this Control control, string name) {
         if (!try_get_template_child(control, name, out T result))
            throw new InvalidOperationException($"Template must have a {typeof(T)} child named '{name}'");
         return result;
      }

      public static IEnumerable<DependencyObject> visual_children(this FrameworkElement v) {
         var count = VisualTreeHelper.GetChildrenCount(v);
         for (var x = 0; x < count; x++) {
            yield return VisualTreeHelper.GetChild(v, x);
         }
      }

      // TODO: replace this method with 'argb_color'
      // - before that, need to verify that no settings/colors become 'invisible' as a result.
      public static Color rgb_color(this UInt32 color) {
         var bytes = BitConverter.GetBytes(color);
         return Color.FromRgb(bytes[2], bytes[1], bytes[0]);
      }

      public static Color argb_color(this uint color) {
         var bytes = BitConverter.GetBytes(color);
         return Color.FromArgb(bytes[3], bytes[2], bytes[1], bytes[0]);
      }

      public static Brush rgb_brush(this uint color) {
         var brush = new SolidColorBrush(rgb_color(color));
         brush.Freeze();
         return brush;
      }

      public static SolidColorBrush rgb_solid_brush(this uint color) =>
         new SolidColorBrush(rgb_color(color));

      public static UInt32 rgb_color(this Color color) {
         return (uint)color.A << 24 | (uint)color.R << 16 | (uint)color.G << 8 | (uint)color.B;
      }

      public static Brush to_brush(this Color color) =>
         new SolidColorBrush(color);

      public static SolidColorBrush to_solid_brush(this Color color) =>
         new SolidColorBrush(color);

      public static Brush frozen(this Brush brush) {
         brush.Freeze();
         return brush;
      }

      public static SolidColorBrush frozen(this SolidColorBrush brush) {
         brush.Freeze();
         return brush;
      }

      public static Color blend(this Color color, Color other) {
         // it doesn't make sense to avg the Alpha value (layering transparent over an opaque color should yield the opaque color).
         // just apply the max Alpha to the resultant color
         if (color.A == 0)
            return other;
         if (other.A == 0)
            return color;
         var color_opac = color.A / 255.0;
         var other_opac = other.A / 255.0;
         var avg_red = (byte)new[] { (color.R, color.A), (other.R, other.A) }.weighted_average(tup => tup.R, tup => tup.A);
         var avg_green = (byte)new[] { (color.G, color.A), (other.G, other.A) }.weighted_average(tup => tup.G, tup => tup.A);
         var avg_blue = (byte)new[] { (color.B, color.A), (other.B, other.A) }.weighted_average(tup => tup.B, tup => tup.A);
         return Color.FromArgb(Math.Max(color.A, other.A), avg_red, avg_green, avg_blue);
      }

      public static Color with(this Color color, byte? alpha = null, byte? red = null, byte? green = null, byte? blue = null) =>
         Color.FromArgb(alpha ?? color.A, red ?? color.R, green ?? color.G, blue ?? color.B);

      public static SolidColorBrush with(this SolidColorBrush brush, double opacity) {
         brush.Opacity = opacity;
         return brush;
      }

      public static Pen frozen(this Pen pen) {
         pen.Freeze();
         return pen;
      }

      public static ImageSource frozen(this ImageSource image_source) {
         image_source.Freeze();
         return image_source;
      }

      public static Point within_bounds(this Point point, double x_min = 0, double y_min = 0, double x_max = double.PositiveInfinity, double y_max = double.PositiveInfinity) =>
         new Point(MathUtils.within_range(x_min, x_max, point.X), MathUtils.within_range(y_min, y_max, point.Y));

      // Perform a breadth first traversal of all children returning any that match the predicate.
      // Matching children are *not* further traversed
      public static IEnumerable<FrameworkElement> visual_children_where(this FrameworkElement v, Func<FrameworkElement, bool> match) {
         var children = visual_children(v).OfType<FrameworkElement>().ToList();
         while (children.Count() > 0) {
            var next = new List<FrameworkElement>();
            foreach (var c in children) {
               if (match(c)) {
                  yield return c;
               } else {
                  next.AddRange(visual_children(c).OfType<FrameworkElement>());
               }
            }
            children = next;
         }
      }

      public static IEnumerable<T> descendants_of_type<T>(this FrameworkElement v) {
         return v.visual_children_where(fe => fe is T).Cast<T>();
      }

      public static bool try_find_descendent_of_type<T>(this FrameworkElement element, out T result) where T : DependencyObject =>
         element.descendants_of_type<T>().TryGetFirst(out result);

      public static bool try_find_ancestor_of_type<T>(this FrameworkElement element, out T result) where T : DependencyObject {
         result = find_ancestor_of_type<T>(element);
         return result != null;
      }

      public static bool try_find_ancestor_of_type<T>(this FrameworkContentElement element, out T result) where T : DependencyObject {
         result = find_ancestor_of_type<T>(element);
         return result != null;
      }

      public static T find_ancestor_of_type<T>(this FrameworkElement element) where T : DependencyObject {
         return candidates().ExceptNull().Distinct().Select(impl).ExceptNull().FirstOrDefault();
         T impl(DependencyObject d) {
            if (d is T direct_match)
               return direct_match;
            if (d is FrameworkElement el) {
               if (el.find_ancestor_of_type<T>() is T rv)
                  return rv;
            } else if (d is FrameworkContentElement cel) {
               if (cel.find_ancestor_of_type<T>() is T rv)
                  return rv;
            }
            return null;
         }
         IEnumerable<DependencyObject> candidates() {
            yield return element.Parent;
            yield return element.TemplatedParent;
            yield return VisualTreeHelper.GetParent(element);
         }
      }

      public static T find_ancestor_of_type<T>(this FrameworkContentElement element) where T : DependencyObject {
         return candidates().ExceptNull().Distinct().Select(impl).ExceptNull().FirstOrDefault();
         T impl(DependencyObject d) {
            if (d is T direct_match)
               return direct_match;
            if (d is FrameworkElement el) {
               if (el.find_ancestor_of_type<T>() is T rv)
                  return rv;
            } else if (d is FrameworkContentElement cel) {
               if (cel.find_ancestor_of_type<T>() is T rv)
                  return rv;
            }
            return null;
         }
         IEnumerable<DependencyObject> candidates() {
            yield return element.Parent;
            yield return element.TemplatedParent;
            yield return VisualTreeHelper.GetParent(element);
         }
      }

      public static void clear_bindings(this DependencyObject d_obj, params DependencyProperty[] d_props) {
         foreach (var dp in d_props)
            BindingOperations.ClearBinding(d_obj, dp);
      }

      public static IDisposable attach_binding(this DependencyObject target, DependencyProperty target_property, string path, FrameworkElement source_view, IValueConverter converter = null) {
         object source_vm = source_view.DataContext;
         try_reset_binding();
         source_view.DataContextChanged += on_data_context_changed;
         return DisposableFactory.Create(() => {
            try {
               source_view.DataContextChanged -= on_data_context_changed;
            } catch (Exception ex) {
               Debug.Assert(false, $"Error detaching from {nameof(source_view)}.{nameof(FrameworkElement.DataContextChanged)} event: {ex}.");
            }
         });
         void on_data_context_changed(object sender, DependencyPropertyChangedEventArgs e) {
            source_vm = e.NewValue;
            try_reset_binding();
         }
         void try_reset_binding() {
            if (source_vm != null)
               target.reset_binding(target_property, new Binding(path) { Source = source_vm, Converter = converter });
         }
      }

      public static void reset_binding(this DependencyObject target, DependencyProperty target_property, BindingBase binding) {
         BindingOperations.ClearBinding(target, target_property);
         BindingOperations.SetBinding(target, target_property, binding);
      }

      public static void TryRaiseCanExecuteChanged(this ICommand command) {
         if (command == null) return;
         else if (command is IRaiseCanExecuteChangedCommand temp)
            temp.RaiseCanExecuteChanged();
         else
            throw new NotImplementedException("This ICommand does not implement IRaiseCanExecuteChanged");
      }

      public static Task ExecuteAsync(this ICommand command, object parameter) {
         ExceptionAssert.Argument.NotNull(command, nameof(command));
         if (command is IAsyncCommand acmd)
            return acmd.Execute(parameter);
         else
            return Task.Run(() => command.Execute(parameter));
      }

      public static void TryExecuteIfCan(this ICommand command, object parameter) {
         if (command != null)
            ExecuteIfCan(command, parameter);
      }

      public static void ExecuteIfCan(this ICommand command, object parameter) {
         ExceptionAssert.Argument.NotNull(command, nameof(command));
         if (command.CanExecute(parameter))
            command.Execute(parameter);
      }

      public static IDisposable ObserveProperty(this DependencyObject instance, DependencyProperty property, EventHandler callback) {
         var descriptor = DependencyPropertyDescriptor.FromProperty(property, instance.GetType());
         if (descriptor != null) {
            descriptor.AddValueChanged(instance, callback);
            return DisposableFactory.Create(() => descriptor.RemoveValueChanged(instance, callback));
         }
         throw new ArgumentException("Could not find property descriptor given type");
      }

      public static IObservable<T> Observe<T>(this DependencyObject instance, DependencyProperty property) {
         Debug.Assert(typeof(T) == property.PropertyType);
         return Observable.Create<T>(observer => {
            return instance.ObserveProperty(property, (o, e) => {
               observer.OnNext((T)instance.GetValue(property));
            });
         });
      }

      public static void CommitAny(this IEditableCollectionView collection) {
         if (collection.IsEditingItem) {
            collection.CommitEdit();
         } else if (collection.IsAddingNew) {
            collection.CommitNew();
         }
      }

      public static void CancelAny(this IEditableCollectionView collection) {
         if (collection.IsEditingItem) {
            if (collection.CanCancelEdit)
               collection.CancelEdit();
         } else if (collection.IsAddingNew) {
            collection.CancelNew();
         }
      }

      public static void ClearMouseBindings(this UIElement element, MouseAction action) =>
         ClearMouseBindings(element, action, ModifierKeys.None);

      public static void ClearMouseBindings(this UIElement element, MouseAction action, ModifierKeys modifiers) {
         for (int i = 0; i < element.InputBindings.Count; i++) {
            var mb = element.InputBindings[i] as MouseBinding;
            if (mb != null) {
               var g = mb.Gesture as MouseGesture;
               if (g != null && g.MouseAction == action && g.Modifiers == modifiers)
                  element.InputBindings.RemoveAt(i);
            }
         }
      }

      public static void ClearKeyBindings(this UIElement element, Key key) =>
         ClearKeyBindings(element, key, ModifierKeys.None);

      public static void ClearKeyBindings(this UIElement element, Key key, ModifierKeys modifiers) {
         for (int i = 0; i < element.InputBindings.Count; i++) {
            if (element.InputBindings[i] is KeyBinding kb && kb.Key == key && kb.Modifiers == modifiers)
               element.InputBindings.RemoveAt(i--);
         }
      }

      public static void ClearCommandBindings(this UIElement element, RoutedCommand cmd) {
         for (int i = 0; i < element.CommandBindings.Count; i++) {
            if (element.CommandBindings[i].Command == cmd)
               element.CommandBindings.RemoveAt(i--);
         }
      }

      public static Size MeasureText(this Typeface font, double size, string text) {
#pragma warning disable CS0618 // Type or member is obsolete
         var ft = new FormattedText(text, System.Globalization.CultureInfo.CurrentCulture, System.Windows.FlowDirection.LeftToRight, font, size, Brushes.Black);
#pragma warning restore CS0618 // Type or member is obsolete
         return new Size(ft.Width, ft.Height);
      }

      public static void SetSortingCriteria(this ICollectionView collection_view, IEnumerable<SortingCriteria> sorting_criteria) {
         collection_view.SortDescriptions.Clear();
         if (sorting_criteria != null)
            foreach (var sc in sorting_criteria)
               collection_view.SortDescriptions.Add(new SortDescription(sc.col_name, sc.is_ascending ? ListSortDirection.Ascending : ListSortDirection.Descending));
      }

      /// <summary>
      /// Converts <paramref name="collection_view"/>.SortDescriptions into a List&lt;<see cref="SortingCriteria"/>&gt;.
      /// </summary>
      public static List<SortingCriteria> get_sorting_criteria(this ICollectionView collection_view, ObservableCollection<ColumnBase> columns) =>
         collection_view.SortDescriptions
         .Select(sd => new SortingCriteria(columns.FirstOrDefault(c => c.SortMemberPath == sd.PropertyName)?.ID, sd.Direction == ListSortDirection.Ascending))
         .Where(sc => sc.col_name != null)
         .ToList();

      public static IEnumerable<SortingCriteria> get_sorting_criteria(IEnumerable<SortDescription> sort_descriptions, IEnumerable<(string id, string path)> columns) =>
         from sd in sort_descriptions
         join col in columns
         on sd.PropertyName equals col.path
         select new SortingCriteria(col.id, sd.Direction == ListSortDirection.Ascending);

      public static IEnumerable<SortDescription> get_sort_descriptions(this IEnumerable<SortingCriteria> sorting_criterion, IEnumerable<(string id, string path)> columns) =>
         from sc in sorting_criterion
         join col in columns
         on sc.col_name equals col.id
         select new SortDescription(col.path, sc.direction());

      /// <summary>
      /// Converts each <see cref="SortingCriteria"/> into a <see cref="SortDescription"/> and assigns the result to <paramref name="collection_view"/>.SortDescriptions.
      /// </summary>
      public static void set_sort_descriptions(this ICollectionView collection_view, IEnumerable<SortingCriteria> sorting_criterion, ObservableCollection<ColumnBase> columns) {
         using (collection_view.DeferRefresh()) {
            collection_view.SortDescriptions.SetItems(sorting_criterion.empty_if_null()
               .Select(sc => new SortDescription(
                  columns.FirstOrDefault(c => string.Equals(c.ID, sc.col_name, StringComparison.CurrentCultureIgnoreCase))?.SortMemberPath,
                  sc.is_ascending ? ListSortDirection.Ascending : ListSortDirection.Descending))
                  .Where(sd => !string.IsNullOrEmpty(sd.PropertyName)));
         }
      }

      public static IObservable<IReadOnlyList<SortingCriteria>> subscribe_sorting_criterion(this ICollectionView collection_view, Func<string, string> sort_member_to_id) =>
         Observable.Create<IReadOnlyList<SortingCriteria>>(o => {
            INotifyCollectionChanged notification_src = collection_view.SortDescriptions;
            IList<SortDescription> sort_descriptions = collection_view.SortDescriptions;
            var sorting_criterion = sort_descriptions.Select(to_criteria).ToList();
            notification_src.CollectionChanged += on_sort_descriptions_changed;
            o.OnNext(sorting_criterion);
            return DisposableFactory.Create(dispose);
            // local methods
            void on_sort_descriptions_changed(object sender, NotifyCollectionChangedEventArgs e) {
               CollectionHelper.reflect_change(sorting_criterion, collection_view.SortDescriptions, to_criteria, null, e);
               o.OnNext(sorting_criterion);
            }
            SortingCriteria to_criteria(SortDescription sd) => new SortingCriteria(sort_member_to_id(sd.PropertyName), sd.Direction == ListSortDirection.Ascending);
            void dispose() => notification_src.CollectionChanged -= on_sort_descriptions_changed;
         });

      public static IEnumerable<object> FlatItems(this CollectionViewGroup group) {
         foreach (var item in group.Items) {
            if (item is CollectionViewGroup nested) {
               foreach (var ni in FlatItems(nested))
                  yield return ni;
            } else {
               yield return item;
            }
         }
      }

      public static Rect Expanded(this Rect rect, double uniform_dimension) {
         // since Rect is a struct, we are mutating a copy of the caller's actual rect
         rect.Inflate(new Size(uniform_dimension, uniform_dimension));
         return rect;
      }
   }
}
