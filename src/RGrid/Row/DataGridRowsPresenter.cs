using Disposable.Extensions;
using Disposable.Extensions.Utilities;
using Monitor.Render.Utilities;
using RGrid.Utility;
using RGrid.WPF;
using RGrid.WPF.Converters;
using System;
using System.Collections;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Shapes;

namespace RGrid {
   delegate void GroupItemSelectionPivotEventHandler(object sender, GroupItemSelectionPivotEventArgs e);

   class DataGridRowsPresenter : ListBox {
      static readonly PropertyPath
         _highlight_path = new PropertyPath(DataGrid.SelectionHighlightProperty),
         _active_path = new PropertyPath(IsSelectionActiveProperty);

      static readonly IValueConverter
         _active_opacity_converter = ValueConverter.create<bool, double>(is_active => is_active ? 1.0 : 0.5);

      double _row_height = DataGrid.DefaultRowHeight;
      ScrollContentPresenter _content_presenter_backing;
      DataGrid _grid;
      AdornerHost<Rectangle> _drag_rect_adorner;
      RowSelectionAdorner _rsa;
      DataGridCellHoverAdorner _cell_hover_adorner;
      ScrollViewer _scroll_viewer;
      Point? _drag_start, _drag_stop;
      double
         _drag_start_horizontal_drift,
         _drag_start_vertical_drift;
      bool _support_group_items;
      int _drag_start_index;
      object _drag_start_item;
      IDisposable _drag_hook;

      static DataGridRowsPresenter() =>
         DefaultStyleKeyProperty.OverrideMetadata(typeof(DataGridRowsPresenter), new FrameworkPropertyMetadata(typeof(DataGridRowsPresenter)));

      public DataGridRowsPresenter() {
         Loaded += on_loaded;
         Unloaded += on_unloaded;
         //
         void on_loaded(object sender, RoutedEventArgs e) {
            destroy_state();
            if (_grid != null)
               _grid.RowHeightChanged -= on_row_height_changed;
            _grid = this.find_ancestor_of_type<DataGrid>();
            _row_height = _grid.RowHeight;
            _grid.RowHeightChanged += on_row_height_changed;
            _support_group_items = _grid.SupportGroupItems;
            _grid.SupportGroupItemsChanged += on_support_group_items_changed;
            _scroll_viewer = this.descendants_of_type<ScrollViewer>().First();
            // selection highlighting
            _rsa = new RowSelectionAdorner(this, _scroll_viewer);
            _rsa.bind(RowSelectionAdorner.FillProperty, _highlight_path, DataGrid.SelectionHighlightBrushConverter, _grid);
            _rsa.bind(RowSelectionAdorner.StrokeProperty, _highlight_path, DataGrid.SelectionHighlightPenConverter, _grid);
            _rsa.bind(OpacityProperty, _active_path, DataGrid.SelectionHighlightOpacityConverter, this);
            _rsa.attach();
            // cell hover adorner
            _cell_hover_adorner = new DataGridCellHoverAdorner(this, _scroll_viewer);
            _cell_hover_adorner.SetBinding(VisibilityProperty, new Binding {
               Path = new PropertyPath(IsMouseOverProperty),
               Converter = new BooleanToVisibilityConverter(),
               Source = this
            });
            // drag
            _drag_hook = DisposableFactory.Create(
               DragDropHelper.subscribe_drag_coordinates(this, init_element: _content_presenter, use_preview_events: true)
               .Subscribe(pt => {
                  if (_drag_start.HasValue) {
                     if (pt.HasValue) {
                        _drag_stop = pt.Value;
                        _refresh_drag_rect();
                     } else {
                        if (_drag_stop.HasValue) {
                           _end_drag(_drag_stop.Value);
                        }
                        _drag_start = null;
                        _drag_stop = null;
                     }
                  } else if (pt.HasValue) {
                     _begin_drag_selection(pt.Value);
                  }
               }),
            _scroll_viewer.SubscribeScrollChanged((s, _) => _refresh_drag_rect()));
         }

         void on_unloaded(object sender, RoutedEventArgs e) =>
            destroy_state();

         // idempotent
         void destroy_state() {
            DisposableUtils.Dispose(ref _drag_hook);
            if (_drag_rect_adorner != null) {
               _drag_rect_adorner.detach();
               _drag_rect_adorner = null;
            }
            if (_rsa != null) {
               _rsa.detach();
               DisposableUtils.Dispose(ref _rsa);
            }
            if (_cell_hover_adorner != null) {
               _cell_hover_adorner.detach();
               DisposableUtils.Dispose(ref _cell_hover_adorner);
            }
         }

         void on_row_height_changed(double v) => _row_height = v;
         void on_support_group_items_changed(bool v) => _support_group_items = v;
      }

      public event GroupItemSelectionPivotEventHandler GroupItemSelectionPivot;

      ScrollContentPresenter _content_presenter => _content_presenter_backing ??
         (_content_presenter_backing = this.descendants_of_type<ScrollContentPresenter>().FirstOrDefault());

      public new void SetSelectedItems(IEnumerable items) =>
         base.SetSelectedItems(items);

      public new void UnselectAll() {
         base.UnselectAll();
      }

      void _begin_drag_selection(Point pt) {
         _drag_rect_adorner?.detach();
         if (InputHitTest(pt) is UIElement el) {
            var sb = new StringBuilder("beginning drag selection on ");
            OneOf.MatchFirst<DependencyObject, ListBoxItem, GroupItem>(el.VisualAncestors().Prepend(el)).Match(init_common, on_group);
            void on_group(GroupItem g) {
               if (g.DataContext is CollectionViewGroup gvm && gvm.FlatItems().Select(ItemContainerGenerator.ContainerFromItem).OfType<ListBoxItem>().TryGetFirst(out var lbi)) {
                  // ListBox calls this internally when a ListBoxItem is clicked;
                  // it needs to be called explicitly in this case, since the click target is a GroupItem
                  Mouse.Capture(this, CaptureMode.SubTree);
                  sb.Append($"{nameof(GroupItem)} '{gvm.Name}' with actual item ");
                  init_common(lbi);
               }
            }
            void init_common(ListBoxItem item_container) {
               // drag state
               _drag_start = pt;
               _drag_stop = pt;
               _drag_start_item = ItemContainerGenerator.ItemFromContainer(item_container);
               _drag_start_index = ItemContainerGenerator.IndexFromContainer(item_container);
               _drag_start_horizontal_drift = _scroll_viewer.HorizontalOffset;
               _drag_start_vertical_drift = _scroll_viewer.VerticalOffset;
               if (!IsKeyboardFocusWithin)
                  Keyboard.Focus(item_container);
               // drag rect
               _drag_rect_adorner = new AdornerHost<Rectangle>(_content_presenter ?? (UIElement)this) {
                  Child = new Rectangle {
                     HorizontalAlignment = HorizontalAlignment.Left,
                     VerticalAlignment = VerticalAlignment.Top,
                     StrokeThickness = DPIUtils.pixel_unit
                  },
                  IsClipEnabled = true
               };
               _drag_rect_adorner.Child.bind(Shape.FillProperty, _highlight_path, DataGrid.SelectionHighlightBrushConverter, _grid);
               _drag_rect_adorner.Child.bind(Shape.StrokeProperty, _highlight_path, DataGrid.SelectionHighlightOpaqueBrushConverter, _grid);
               _refresh_drag_rect();
               _drag_rect_adorner.attach();
               // log
               sb.Append($"'{_drag_start_item}' (selection mode {SelectionMode}).");
               Debug.WriteLine(sb.ToString());
            }
         }
      }

      void _end_drag(Point drag_stop) {
         if (_drag_rect_adorner != null) {
            _drag_rect_adorner.detach();
            _drag_rect_adorner = null;
         }
         if (_drag_start.HasValue) {
            Debug.WriteLine("ending drag selection on mouse up.");
            if (SelectionMode != SelectionMode.Single && _content_presenter != null) {
               drag_stop.X = MathUtils.within_range(0, _content_presenter.RenderSize.Width - 1, drag_stop.X);
               drag_stop.Y = MathUtils.within_range(0, _content_presenter.RenderSize.Height - 1, drag_stop.Y);
               double vertical_diff = drag_stop.Y + (_row_height * (_scroll_viewer.VerticalOffset - _drag_start_vertical_drift)) - _drag_start.Value.Y;
               int start_index, n_items;
               if (vertical_diff > 0) {
                  double first_item_partial_height = _row_height - (_drag_start.Value.Y % _row_height);
                  n_items = (int)Math.Ceiling((vertical_diff - first_item_partial_height) / _row_height) + 1;
                  start_index = _drag_start_index;
                  int overflow = start_index + n_items - Items.Count;
                  if (overflow > 0)
                     n_items -= overflow;
               } else {
                  double first_item_partial_height = _drag_start.Value.Y % _row_height;
                  n_items = -(int)Math.Floor((vertical_diff + first_item_partial_height) / _row_height) + 1;
                  start_index = _drag_start_index - n_items + 1;
                  if (start_index < 0) {
                     n_items += start_index;
                     start_index = 0;
                  }
               }
               if (n_items > 1) {
                  if (_support_group_items) {
                     if (this.ChildAtPoint(drag_stop, out ListBoxItem drag_stop_container)) {
                        _select_items_between(_drag_start_item, ItemContainerGenerator.ItemFromContainer(drag_stop_container));
                     } else if (this.ChildAtPoint(drag_stop, out GroupItem gi) && gi.DataContext is CollectionViewGroup cvg && cvg.ItemCount > 0) {
                        if ((ItemContainerGenerator.ContainerFromItem(_drag_start_item)?.VisualAncestors<GroupItem>()).empty_if_null().FirstOrDefault() == gi) {
                           _select_items_between(_drag_start_item, cvg.Items.FirstOrDefault());
                        } else {
                           var gisp_args = new GroupItemSelectionPivotEventArgs(PreviewMouseUpEvent, this, cvg);
                           GroupItemSelectionPivot?.Invoke(this, gisp_args);
                           if (gisp_args.Handled) {
                              // don't select the inner group items
                              if (vertical_diff < 0) {
                                 _select_items_between(_drag_start_item, cvg.FlatItems().FirstOrDefault());
                              } else {
                                 var end = cvg.FlatItems().FirstOrDefault();
                                 var selection = Items.Cast<object>()
                                    .Between(_drag_start_item, cvg.FlatItems().FirstOrDefault())
                                    .Prepend(_drag_start_item);
                                 Debug.WriteLine($"selecting item range (inclusive begin '{_drag_start_item}' to exclusive end {end}).");
                                 SetSelectedItems(selection);
                              }
                           } else {
                              /// Execute default behavior (select all the inner items in the GroupItem)
                              /// The way <see cref="_select_items_between(object, object)"/> works, is two items play the role of 'bookends' - every item in between them is selected.
                              /// Since the drag_start item is the first bookend, depending on the drag direction, either the first, or last item in the group will be the other bookend.
                              _select_items_between(_drag_start_item, vertical_diff > 0 ? cvg.Items.Last() : cvg.Items.First());
                           }
                        }
                     }
                  } else {
                     var selection = Enumerable.Range(start_index, n_items).Select(i => Items[i]).ToList();
                     Debug.Assert(selection.Count > 0);
                     Debug.WriteLine($"selecting item range (size {selection.Count}) determined by drag distance '{vertical_diff}' x row height '{_row_height}' (inclusive begin '{selection[0]}' to inclusive end '{selection[selection.Count - 1]}').");
                     SetSelectedItems(selection);
                  }
               }
            }
            _drag_start = null;
            _drag_start_item = null;
         }
      }

      void _refresh_drag_rect() {
         if (_drag_rect_adorner == null || !_drag_start.HasValue || !_drag_stop.HasValue)
            return;
         var hdiff = _scroll_viewer.HorizontalOffset - _drag_start_horizontal_drift;
         var vdiff = _scroll_viewer.VerticalOffset - _drag_start_vertical_drift;
         var rect = new Rect(
            new Point(_drag_start.Value.X - hdiff, _drag_start.Value.Y - vdiff * _grid.RowHeight),
            _drag_stop.Value);
         if (rect.IsEmpty)
            return;
         rect.Intersect(this.GetChildBounds(_drag_rect_adorner.AdornedElement));
         _drag_rect_adorner.Child.Margin = new Thickness(rect.Left, rect.Top, 0, 0);
         _drag_rect_adorner.Child.Width = rect.Width;
         _drag_rect_adorner.Child.Height = rect.Height;
      }

      void _select_items_between(object inclusive_begin, object inclusive_end) {
         if (inclusive_begin == null || inclusive_end == null) {
            // One could argue that an exception should be thrown here, as null is a bogus value for either parameter.
            // However, clearing the selection seems reasonable enough, and does not risk crashing (assuming nothing particularly nefarious is happening).
            Debug.WriteLine($"clearing the selection because the specified item range cannot be determined (inclusive begin '{inclusive_begin}' to inclusive end '{inclusive_end}').");
            UnselectAll();
         } else if (inclusive_begin == inclusive_end) {
            Debug.WriteLine($"selecting single item '{inclusive_begin}.'");
            SetSelectedItems(new[] { inclusive_begin });
         } else {
            Debug.WriteLine($"selecting item range (inclusive begin '{inclusive_begin}' to inclusive end '{inclusive_end}').");
            SetSelectedItems(Items.Cast<object>()
               .Between(inclusive_begin, inclusive_end)
               .BookEnd(inclusive_begin, inclusive_end));
         }
      }
   }

   //class DataGridRowsPresenter : ListBox {
   //   double _row_height = DataGrid.DefaultRowHeight;
   //   DataGrid _grid;
   //   DragRectAdorner _drag_rect_adorner;
   //   ScrollViewer _scroll_viewer;
   //   Point? _drag_start;
   //   double _drag_start_vertical_drift;
   //   int _drag_start_index;

   //   static DataGridRowsPresenter() =>
   //      DefaultStyleKeyProperty.OverrideMetadata(typeof(DataGridRowsPresenter), new FrameworkPropertyMetadata(typeof(DataGridRowsPresenter)));

   //   public DataGridRowsPresenter() {
   //      Loaded += (s, e) => {
   //         if (_grid != null)
   //            _grid.RowHeightChanged -= on_row_height_changed;
   //         _grid = this.find_ancestor_of_type<DataGrid>();
   //         _row_height = _grid.RowHeight;
   //         _grid.RowHeightChanged += on_row_height_changed;
   //         _scroll_viewer = this.descendants_of_type<ScrollViewer>().First();
   //         if (SelectionMode != SelectionMode.Single) {
   //            _drag_rect_adorner = new DragRectAdorner(this, _scroll_viewer, _row_height);
   //            _drag_rect_adorner.SetResourceReference(DragRectAdorner.FillProperty, DataGrid.ActiveSelectionHighlightKey);
   //            _drag_rect_adorner.SetResourceReference(DragRectAdorner.StrokeProperty, DataGrid.SelectionStrokeKey);
   //            _drag_rect_adorner.attach();
   //         }
   //      };
   //      Unloaded += (s, e) => {
   //         if (_drag_rect_adorner != null) {
   //            _drag_rect_adorner.detach();
   //            DisposableUtils.Dispose(ref _drag_rect_adorner);
   //         }
   //      };
   //      void on_row_height_changed(double v) => _row_height = v;
   //   }


   //   protected override void OnPreviewMouseLeftButtonDown(MouseButtonEventArgs e) {
   //      var pt = e.GetPosition(this);
   //      var container = this.ChildAtPoint<ListBoxItem>(pt);
   //      if (container != null) {
   //         var item = ItemContainerGenerator.ItemFromContainer(container);
   //         if (item != null) {
   //            _drag_start = pt;
   //            _drag_start_index = Items.IndexOf(item);
   //            _drag_start_vertical_drift = _scroll_viewer.VerticalOffset;
   //         }
   //      }
   //      base.OnPreviewMouseLeftButtonDown(e);
   //   }

   //   protected override void OnPreviewMouseUp(MouseButtonEventArgs e) {
   //      if (SelectionMode != SelectionMode.Single) {
   //         if (_drag_start.HasValue) {
   //            var drag_stop = e.GetPosition(this);
   //            double vertical_diff = drag_stop.Y + (_row_height * (_scroll_viewer.VerticalOffset - _drag_start_vertical_drift)) - _drag_start.Value.Y;
   //            int start_index, n_items;
   //            if (vertical_diff > 0) {
   //               double first_item_partial_height = _row_height - (_drag_start.Value.Y % _row_height);
   //               n_items = (int)Math.Ceiling((vertical_diff - first_item_partial_height) / _row_height) + 1;
   //               start_index = _drag_start_index;
   //               int overflow = start_index + n_items - Items.Count;
   //               if (overflow > 0)
   //                  n_items -= overflow;
   //            } else {
   //               double first_item_partial_height = _drag_start.Value.Y % _row_height;
   //               n_items = -(int)Math.Floor((vertical_diff + first_item_partial_height) / _row_height) + 1;
   //               start_index = _drag_start_index - n_items + 1;
   //               if (start_index < 0) {
   //                  n_items += start_index;
   //                  start_index = 0;
   //               }
   //            }
   //            if (n_items > 1) {
   //               var selection = Enumerable.Range(start_index, n_items).Select(i => Items[i]).ToList();
   //               SetSelectedItems(selection);
   //            }
   //            _drag_start = null;
   //         }
   //      }
   //      base.OnPreviewMouseUp(e);
   //   }

   //   protected override DependencyObject GetContainerForItemOverride() => new DataGridRow();
   //}



   //public class DataGridRowsPresenter : ListBox {
   //   double _row_height = DataGrid.DefaultRowHeight;
   //   DataGrid _grid;
   //   DragRectAdorner _drag_rect_adorner;
   //   ScrollViewer _scroll_viewer;
   //   Point? _drag_start;
   //   double _drag_start_vertical_drift;
   //   int _drag_start_index;

   //   static DataGridRowsPresenter() =>
   //      DefaultStyleKeyProperty.OverrideMetadata(typeof(DataGridRowsPresenter), new FrameworkPropertyMetadata(typeof(DataGridRowsPresenter)));

   //   public DataGridRowsPresenter() {
   //      Loaded += (s, e) => {
   //         if (_grid != null)
   //            _grid.RowHeightChanged -= on_row_height_changed;
   //         _grid = this.find_ancestor_of_type<DataGrid>();
   //         _row_height = _grid.RowHeight;
   //         _grid.RowHeightChanged += on_row_height_changed;
   //         _scroll_viewer = this.descendants_of_type<ScrollViewer>().First();
   //         _drag_rect_adorner = new DragRectAdorner(this, _scroll_viewer, _row_height);
   //         _drag_rect_adorner.SetResourceReference(DragRectAdorner.FillProperty, DataGrid.ActiveSelectionHighlightKey);
   //         _drag_rect_adorner.SetResourceReference(DragRectAdorner.StrokeProperty, DataGrid.SelectionStrokeKey);
   //         _drag_rect_adorner.attach();
   //      };
   //      Unloaded += (s, e) => {
   //         if (_drag_rect_adorner != null) {
   //            _drag_rect_adorner.detach();
   //            Disposable.dispose(ref _drag_rect_adorner);
   //         }
   //      };
   //      void on_row_height_changed(double v) => _row_height = v;
   //   }


   //   protected override void OnPreviewMouseLeftButtonDown(MouseButtonEventArgs e) {
   //      var pt = e.GetPosition(this);
   //      var container = this.ChildAtPoint<ListBoxItem>(pt);
   //      if (container != null) {
   //         var item = ItemContainerGenerator.ItemFromContainer(container);
   //         if (item != null) {
   //            _drag_start = pt;
   //            _drag_start_index = Items.IndexOf(item);
   //            _drag_start_vertical_drift = _scroll_viewer.VerticalOffset;
   //         }
   //      }
   //      base.OnPreviewMouseLeftButtonDown(e);
   //   }

   //   protected override void OnPreviewMouseUp(MouseButtonEventArgs e) {
   //      if (_drag_start.HasValue) {
   //         var drag_stop = e.GetPosition(this);
   //         double vertical_diff = drag_stop.Y + (_row_height * (_scroll_viewer.VerticalOffset - _drag_start_vertical_drift)) - _drag_start.Value.Y;
   //         int start_index, n_items;
   //         if (vertical_diff > 0) {
   //            double first_item_partial_height = _row_height - (_drag_start.Value.Y % _row_height);
   //            n_items = (int)Math.Ceiling((vertical_diff - first_item_partial_height) / _row_height) + 1;
   //            start_index = _drag_start_index;
   //            int overflow = start_index + n_items - Items.Count;
   //            if (overflow > 0)
   //               n_items -= overflow;
   //         } else {
   //            double first_item_partial_height = _drag_start.Value.Y % _row_height;
   //            n_items = -(int)Math.Floor((vertical_diff + first_item_partial_height) / _row_height) + 1;
   //            start_index = _drag_start_index - n_items + 1;
   //            if (start_index < 0) {
   //               n_items += start_index;
   //               start_index = 0;
   //            }
   //         }
   //         if (n_items > 1) {
   //            var selection = Enumerable.Range(start_index, n_items).Select(i => Items[i]).ToList();
   //            SetSelectedItems(selection);
   //         }
   //         _drag_start = null;
   //      }
   //      base.OnPreviewMouseUp(e);
   //   }

   //   protected override DependencyObject GetContainerForItemOverride() => new DataGridRow();
   //}
}