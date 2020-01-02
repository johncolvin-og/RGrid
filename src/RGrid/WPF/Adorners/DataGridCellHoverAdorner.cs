using Disposable.Extensions;
using Disposable.Extensions.Utilities;
using Monitor.Render.Utilities;
using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;

namespace RGrid.WPF {
   class DataGridCellHoverAdorner : Adorner, IDisposable {
      Brush _row_fill, _cell_fill;
      Pen _row_stroke, _cell_stroke;

      ScrollViewer _scroll_viewer;
      ScrollContentPresenter _scroll_content;
      DataGrid _grid;
      IDisposable _dispose;
      CellGeometry? _hover_cell;
      AdornerHost _menu_host = null;

      #region RowFill
      public static readonly DependencyProperty RowFillProperty =
         WPFHelper.create_dp<Brush, DataGridCellHoverAdorner>(
            nameof(RowFill),
            FrameworkPropertyMetadataOptions.AffectsRender,
            (o, v) => o._row_fill = v,
            FrozenBrushCache.get_brush(DataGrid.DefaultSelectionHighlight));

      public SolidColorBrush RowFill {
         get => GetValue(RowFillProperty) as SolidColorBrush;
         set => SetValue(RowFillProperty, value);
      }
      #endregion

      #region RowStroke
      public static readonly DependencyProperty RowStrokeProperty =
         WPFHelper.create_dp<Pen, DataGridCellHoverAdorner>(
            nameof(RowStroke),
            FrameworkPropertyMetadataOptions.AffectsRender,
            (o, v) => o._row_stroke = v);

      public Pen RowStroke {
         get => GetValue(RowStrokeProperty) as Pen;
         set => SetValue(RowStrokeProperty, value);
      }
      #endregion

      #region CellFill
      public static readonly DependencyProperty CellFillProperty =
         WPFHelper.create_dp<Brush, DataGridCellHoverAdorner>(
            nameof(CellFill),
            FrameworkPropertyMetadataOptions.AffectsRender,
            (o, v) => o._cell_fill = v);

      public SolidColorBrush CellFill {
         get => GetValue(CellFillProperty) as SolidColorBrush;
         set => SetValue(CellFillProperty, value);
      }
      #endregion

      #region CellStroke
      public static readonly DependencyProperty CellStrokeProperty =
         WPFHelper.create_dp<Pen, DataGridCellHoverAdorner>(
            nameof(CellStroke),
            FrameworkPropertyMetadataOptions.AffectsRender,
            (o, v) => o._cell_stroke = v);

      public Pen CellStroke {
         get => GetValue(CellStrokeProperty) as Pen;
         set => SetValue(CellStrokeProperty, value);
      }
      #endregion

      public DataGridCellHoverAdorner(ListBox list_box, ScrollViewer scroll_viewer)
         : this(list_box, scroll_viewer, list_box.descendants_of_type<ScrollContentPresenter>().FirstOrDefault()) { }

      /// <summary>
      /// Create an instance of the <see cref="DataGridCellHoverAdorner"/> class.
      /// </summary>
      /// <param name="list_box"></param>
      /// <param name="scroll_viewer"></param>
      /// <param name="scroll_content">
      /// The actual AdornedElement (the ScrollViewer may clip the ScrollContentPresenter, and the most prudent way to reflect such a clip is to use it as the actual AdornedElement).
      /// </param>
      public DataGridCellHoverAdorner(ListBox list_box, ScrollViewer scroll_viewer, ScrollContentPresenter scroll_content)
         : base(scroll_content) {
         _row_fill = RowFill;
         _row_stroke = RowStroke;
         _cell_fill = CellFill;
         _cell_stroke = CellStroke;
         _scroll_viewer = scroll_viewer;
         _scroll_content = scroll_content;
         IsHitTestVisible = false;
         // reflect AdornedElement's clip
         IsClipEnabled = true;

         _grid = list_box.find_ancestor_of_type<DataGrid>();
         _dispose = DisposableFactory.Create(
            _grid.SubscribeMouseHoverCell().Subscribe(cell => {
               _hover_cell = cell;
               if (_hover_cell.HasValue) {
                  var (row, col) = _hover_cell.Value;
                  var row_tl = row.TranslatePoint(default, this);
               }
               InvalidateVisual();
            }));
      }

      protected override void OnRender(DrawingContext drawingContext) {
         base.OnRender(drawingContext);
         if (_hover_cell.HasValue) {
            (var row, var cell) = _hover_cell.Value;
            if (row != null) {
               var row_tl = new Lazy<Point>(() => row.TranslatePoint(default, this));
               if (_row_fill != null || _row_stroke != null) {
                  var row_rect = new Rect(row_tl.Value, row.RenderSize);
                  if (_scroll_content != null) {
                     var content_bounds = this.GetChildBounds(_scroll_content);
                     row_rect.Intersect(content_bounds);
                  }
                  drawingContext.DrawRectangle(_row_fill, _row_stroke, row_rect, true);
               }
               if (_cell_fill != null || _cell_stroke != null) {
                  var cell_rect = cell.to_visible_rect(row.ActualHeight, row_tl.Value.Y);
                  drawingContext.DrawRectangle(_cell_fill, _cell_stroke, cell_rect, true);
               }
            }
         }
      }

      public void Dispose() =>
         DisposableUtils.Dispose(ref _dispose);
   }
}
