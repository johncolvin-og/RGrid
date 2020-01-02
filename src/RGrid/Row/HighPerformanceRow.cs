using RGrid.Controls;
using RGrid.Utility;
using RGrid.WPF;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace RGrid {
   public class HighPerformanceRowBase : FrameworkElement {
      protected Brush cached_background;

      public static readonly DependencyProperty BackgroundProperty =
         WPFHelper.create_dp<Brush, HighPerformanceRowBase>(
            nameof(Background),
            FrameworkPropertyMetadataOptions.AffectsRender,
            (o, v) => o.cached_background = v);

      public Brush Background {
         get => GetValue(BackgroundProperty) as Brush;
         set => SetValue(BackgroundProperty, value);
      }
   }

   public class HighPerformanceRow<TRowVM> : HighPerformanceRowBase where TRowVM : class, IRowVM {
      double _row_height;
      ObservableCollection<ColumnBase> _columns;
      List<ColumnGeometry> _column_layout = new List<ColumnGeometry>();
      Dictionary<string, ColumnGeometry> _column_layout_map = new Dictionary<string, ColumnGeometry>();
      VisualDictionary<string, FrameworkElement> _element_cells;
      RecycleBin<DataGrid> _grid_backing;
      RecycleBin<ListBoxItem> _container_backing;
      TRowVM _row;

      public HighPerformanceRow() : this(DataGrid.DefaultRowHeight) { }
      public HighPerformanceRow(double row_height) {
         _row_height = row_height;
         _element_cells = new VisualDictionary<string, FrameworkElement>(this);
         _grid_backing = new RecycleBin<DataGrid>(
            g => { g.ColumnLayoutChanged += _on_column_layout_changed; g.RowHeightChanged += _on_row_height_changed; },
            g => { g.ColumnLayoutChanged -= _on_column_layout_changed; g.RowHeightChanged -= _on_row_height_changed; });
         _container_backing = new RecycleBin<ListBoxItem>(c => c.SubscribeSizeChanged(InvalidateMeasure));
         Height = _row_height;
         Loaded += _on_loaded;
         Unloaded += _on_unloaded;
         DataContextChanged += _on_data_context_changed;
      }

      DataGrid _grid { get => _grid_backing.value; set => _grid_backing.value = value; }
      ListBoxItem _container { get => _container_backing.value; set => _container_backing.value = value; }

      void _on_data_context_changed(object sender, DependencyPropertyChangedEventArgs e) {
         if (_row != null)
            _row.invalidated -= InvalidateVisual;
         _row = e.NewValue as TRowVM;
         if (_row != null)
            _row.invalidated += InvalidateVisual;
         InvalidateVisual();
      }

      void _on_loaded(object sender, RoutedEventArgs e) {
         _container = this.find_ancestor_of_type<ListBoxItem>();
         _grid = _container.find_ancestor_of_type<DataGrid>();
         _on_row_height_changed(_grid.RowHeight);
         var new_columns = _grid.visible_columns;
         if (new_columns != _columns) {
            if (_columns != null)
               _columns.CollectionChanged -= _on_columns_collection_changed;
            _column_layout = _grid.GetColumnGeometry().ToList();
            _set_column_layout_map();
            _columns = new_columns;
            _on_columns_collection_changed(_columns, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
            _columns.CollectionChanged += _on_columns_collection_changed;
         } else {
            _on_column_layout_changed(_grid.GetColumnGeometry().ToList());
         }
      }

      void _on_unloaded(object sender, RoutedEventArgs e) {
         _container = null;
         _grid = null;
      }

      void _on_row_height_changed(double v) {
         if (_row_height != v) {
            _row_height = Height = v;
            InvalidateMeasure();
            InvalidateArrange();
            InvalidateVisual();
         }
      }

      protected override int VisualChildrenCount => _element_cells.Count;

      protected override Visual GetVisualChild(int index) =>
         _element_cells.VisualAtIndex(index);

      protected override void OnRender(DrawingContext dc) {
         if (cached_background != null) {
            dc.DrawRectangle(cached_background, null, new Rect(0, 0, _column_layout.Sum(col => col.visible_width), ActualHeight));
         }
         if (_row != null) {
            foreach (var cell in _column_layout) {
               if (!cell.column.is_collapsed && cell.column is DataGrid.IRenderColumn<TRowVM> rc)
                  rc.draw(_row, dc, cell, _row_height);
            }
         }
         base.OnRender(dc);
      }

      protected override Size MeasureOverride(Size availableSize) {
         foreach (var kv in _column_layout_map) {
            if (_element_cells.TryGetValue(kv.Key, out FrameworkElement ec))
               ec.Measure(new Size(kv.Value.width, _row_height));
         }
         return RowUtils.measure(_container, _column_layout, availableSize, _row_height);
      }

      protected override Size ArrangeOverride(Size finalSize) {
         foreach (var kv in _column_layout_map) {
            if (_element_cells.TryGetValue(kv.Key, out FrameworkElement ec))
               ec.Arrange(kv.Value.to_rect(_row_height));
         }
         return finalSize;
      }

      #region Private Methods
      void _on_columns_collection_changed(object sender, NotifyCollectionChangedEventArgs e) {
         switch (e.Action) {
            case NotifyCollectionChangedAction.Add: _add_columns(e.NewItems); break;
            case NotifyCollectionChangedAction.Remove: _remove_columns(e.OldItems); break;
            case NotifyCollectionChangedAction.Replace:
               if (e.OldItems != null) _remove_columns(e.OldItems);
               if (e.NewItems != null) _add_columns(e.NewItems);
               break;
            case NotifyCollectionChangedAction.Reset:
               _element_cells.Clear();
               _add_columns((IList)sender);
               break;
            default: return;
         }
         InvalidateArrange();
         InvalidateVisual();
      }

      void _add_columns(IList items) {
         foreach (ColumnBase col in items) {
            if (col is DataGrid.IFrameworkElementColumn el_col) {
               var cell_view = el_col.view_factory();
               DataGrid.CellRender.change_position(cell_view, _row_height, _column_layout_map.GetNullable(col.ID));
               _element_cells.Add(col.ID, cell_view);
            }
         }
      }

      void _remove_columns(IList items) {
         foreach (ColumnBase col in items)
            _element_cells.Remove(col.ID);
      }

      void _on_column_layout_changed(List<ColumnGeometry> column_layout) {
         _column_layout = column_layout;
         _set_column_layout_map();
         foreach (var kv in _element_cells)
            DataGrid.CellRender.change_position(kv.Value, _row_height, _column_layout_map.GetNullable(kv.Key));
         if (_element_cells.Count > 0)
            InvalidateArrange();
         InvalidateVisual();
      }

      void _set_column_layout_map() {
         _column_layout_map = new Dictionary<string, ColumnGeometry>();
         foreach (var cg in _column_layout) {
            if (cg.column is DataGrid.IFrameworkElementColumn)
               _column_layout_map.Add(cg.column.ID, cg);
         }
      }
      #endregion
   }
}
