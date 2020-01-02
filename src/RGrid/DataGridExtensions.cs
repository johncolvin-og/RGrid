using Collections.Sync.Extensions;
using Collections.Sync.Utils;
using Disposable.Extensions;
using Disposable.Extensions.Utilities;
using RGrid.Utility;
using RGrid.WPF;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace RGrid {
   public static class DIYGridExtensions {
      public static IDisposable subscribe_ColumnLayoutChanged<TColKey>(this DataGrid grid, Action<IDictionary<TColKey, ColumnGeometry>> callback) {
         grid.ColumnLayoutChanged += on_ColumnLayoutChanged;
         return DisposableFactory.Create(() => grid.ColumnLayoutChanged -= on_ColumnLayoutChanged);
         void on_ColumnLayoutChanged(List<ColumnGeometry> column_geometry) {
            var map = new Dictionary<TColKey, ColumnGeometry>();
            foreach (var cg in grid.GetColumnGeometry())
               if (cg.column is IKeyedColumn<TColKey> col)
                  map.Add(col.key, cg);
            callback(map);
         }
      }

      public static IObservable<T> SubscribeMouseHoverRow<T>(this DataGrid grid) where T : class =>
         SubscribeMouseHoverRow(grid, EqualityComparer<T>.Default);

      public static IObservable<T> SubscribeMouseHoverRow<T>(this DataGrid grid, IEqualityComparer<T> comparer) where T : class =>
         Observable.Create<T>(o => {
            T last_row = null;
            return grid.SubscribeMouseHoverRowContainer().Subscribe(row_view => {
               if (grid.RowContainerGenerator.ItemFromContainer(row_view) is T row && !comparer.Equals(row, last_row))
                  o.OnNext(last_row = row);
            });
         });

      public static IObservable<ListBoxItem> SubscribeMouseHoverRowContainer(this DataGrid grid) =>
         Observable.Create<ListBoxItem>(o => {
            bool busy = false, dirty = false;
            ListBoxItem curr_row = null;
            return DisposableFactory.Create(
               grid.SubscribeMouseEnter(on_mouse),
               grid.SubscribeMouseLeave(on_mouse),
               grid.SubscribeMouseLeftButtonDown(on_mouse),
               grid.SubscribeMouseLeftButtonUp(on_mouse),
               grid.SubscribeMouseMove(on_mouse));
            void on_mouse(object sender, MouseEventArgs e) => refresh();
            void refresh() {
               if (busy) {
                  dirty = true;
               } else {
                  dirty = false;
                  busy = true;
                  do {
                     grid.Dispatcher.InvokeAsync(() => {
                        var row = grid.ChildAtPoint<ListBoxItem>(Mouse.GetPosition(grid));
                        if (row != curr_row)
                           o.OnNext(curr_row = row);
                        dirty = false;
                     });
                  } while (dirty);
                  busy = false;
               }
            }
         });

      public static IObservable<CellGeometry?> SubscribeMouseHoverCell(this DataGrid grid) {
         return Observable.Create<CellGeometry?>(o => {
            IDisposable row_mouse_hooks = null;
            CellGeometry? last_value = null;
            var layout = grid.GetColumnGeometry().ToList();
            var layout_x_pts = layout.WrapSelector(cg => cg.left, null);
            ListBoxItem hover_row = null;
            grid.ColumnLayoutChanged += on_layout;
            var hover_sub = grid.SubscribeMouseHoverRowContainer().Subscribe(on_row_view);
            return DisposableFactory.Create(dispose);
            //
            void notify(MouseEventArgs e = null) {
               if (hover_row == null || layout.Count == 0)
                  on_cell(null);
               else {
                  var pt = e?.GetPosition(hover_row) ?? Mouse.GetPosition(hover_row);
                  int index = CollectionHelper.binary_search(layout_x_pts, pt.X, Comparer<double>.Default);
                  if (index < 0)
                     index = ~index - 1;
                  if (index >= 0 && index < layout.Count) {
                     on_cell(new CellGeometry(layout[index], hover_row));
                  } else {
                     on_cell(null);
                  }
               }
            }
            void on_cell(CellGeometry? cell) {
               if (!cell.Equals(last_value))
                  o.OnNext(last_value = cell);
            }
            void on_row_view(ListBoxItem row_view) {
               row_mouse_hooks?.Dispose();
               hover_row = row_view;
               row_mouse_hooks = row_view?.SubscribeMouseMove(on_mouse_change);
               notify();
            }
            void on_layout(List<ColumnGeometry> obj) {
               layout = grid.GetColumnGeometry().ToList();
               layout_x_pts = layout.WrapSelector(cg => cg.left, null);
               notify();
            }
            void on_mouse_change(object sender, MouseEventArgs e) =>
               notify(e);
            void dispose() {
               DisposableUtils.Dispose(ref hover_sub);
               grid.ColumnLayoutChanged -= on_layout;
            }
         });
      }

      public static IObservable<(IEnumerable<ColumnGeometry> columns, int row_start, int row_count)?> SubscribeCellDragRect(this DataGrid grid) {
         return Observable.Create<(IEnumerable<ColumnGeometry> columns, int row_start, int row_count)?>(o => {
            return grid.ConnectOnLoaded(init);
            IDisposable init() {
               if (!(grid.try_find_descendent_of_type(out DataGridRowsPresenter presenter) && presenter.try_find_descendent_of_type(out ScrollViewer scroll) && scroll.try_find_descendent_of_type(out ScrollContentPresenter scroll_content)))
                  return null;
               // init state
               var value_container = new DistinctSubject<(IEnumerable<ColumnGeometry> columns, int row_start, int row_count)?>();
               double y_start_drift = 0;
               var layout = new List<ColumnGeometry>();
               var layout_x_pts = layout.WrapSelector(cg => cg.left, null);
               object _drag_start_item = null;
               int _drag_start_index = -1;
               Point? drag_start = null, drag_curr = null;
               //
               scroll.ScrollChanged += on_scroll;
               grid.ColumnLayoutChanged += on_layout;
               var drag_hook = DragDropHelper.subscribe_drag_coordinates(presenter, grid, use_preview_events: false, mouse_button: MouseButton.Right).Subscribe(pt => {
                  if (drag_start.HasValue) {
                     if (!pt.HasValue) {
                        drag_start = null;
                        drag_curr = null;
                     } else {
                        drag_curr = pt;
                     }
                  } else {
                     if (pt.HasValue) {
                        var item_container = presenter.ChildAtPoint<ListBoxItem>(pt.Value);
                        if (item_container == null) {
                           pt = null;
                        } else {
                           _drag_start_item = grid.RowContainerGenerator.ItemFromContainer(item_container);
                           _drag_start_index = grid.RowContainerGenerator.IndexFromContainer(item_container);
                           y_start_drift = scroll.VerticalOffset;
                        }
                     }
                     drag_start = pt;
                     drag_curr = pt;
                  }
                  refresh();
               });
               grid.PreviewMouseUp += (s, e) => e.Handled = e.ChangedButton == MouseButton.Right;
               var sub = value_container.Subscribe(o);
               return DisposableFactory.Create(dispose);
               void on_layout(List<ColumnGeometry> new_layout) {
                  layout = new_layout;
                  layout_x_pts = layout.WrapSelector(cg => cg.left, null);
               }
               void on_scroll(object sender, ScrollChangedEventArgs e) {
                  if (drag_start.HasValue)
                     refresh();
               }

               void refresh() {
                  if (!drag_start.HasValue) {
                     value_container.OnNext(null);
                     return;
                  }
                  var (xstart, ystart) = drag_start.Value.deconstruct();
                  var (xstop, ystop) = drag_curr.Value.deconstruct();
                  xstart = MathUtils.within_range(0, presenter.ActualWidth, xstart);
                  var row_height = grid.RowHeight;
                  xstop = MathUtils.within_range(0, scroll_content.RenderSize.Width - 1, xstop);
                  ystop = MathUtils.within_range(0, scroll_content.RenderSize.Height - 1, ystop);
                  double vertical_diff = ystop + (row_height * (scroll.VerticalOffset - y_start_drift)) - ystart;
                  int start_index, n_items;
                  if (vertical_diff > 0) {
                     double first_item_partial_height = row_height - (ystart % row_height);
                     n_items = (int)Math.Ceiling((vertical_diff - first_item_partial_height) / row_height) + 1;
                     start_index = _drag_start_index;
                     int overflow = start_index + n_items - presenter.Items.Count;
                     if (overflow > 0)
                        n_items -= overflow;
                  } else {
                     double first_item_partial_height = ystart % row_height;
                     n_items = -(int)Math.Floor((vertical_diff + first_item_partial_height) / row_height) + 1;
                     start_index = _drag_start_index - n_items + 1;
                     if (start_index < 0) {
                        n_items += start_index;
                        start_index = 0;
                     }
                  }
                  if (n_items > 0) {
                     var colstart = find_col(xstart);
                     var colstop = find_col(xstop);
                     (colstart, colstop) = MathUtils.get_outliers(colstart, colstop);
                     (ystart, ystop) = MathUtils.get_outliers(ystart, ystop);
                     var cols = layout.WrapSubRange(colstart, colstop - colstart + 1);
                     value_container.OnNext((cols, start_index, n_items));
                  }
               }

               int find_col(double x) {
                  if (layout.Count == 0)
                     return -1;
                  int i = CollectionHelper.binary_search(layout.WrapSelector(l => l.left, null), x, Comparer<double>.Default);
                  if (i < 0) {
                     i = ~i - 1;
                  }
                  return MathUtils.within_range(0, layout.Count - 1, i);
               }

               void dispose() {
                  DisposableUtils.Dispose(ref sub);
                  grid.ColumnLayoutChanged -= on_layout;
               }
            }
         });
      }
   }
}
