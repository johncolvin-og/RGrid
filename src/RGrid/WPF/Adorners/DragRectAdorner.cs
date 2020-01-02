using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;

namespace RGrid.WPF {
   class DragRectAdorner : Adorner, IDisposable {
      Brush _fill;
      Pen _stroke;
      readonly ScrollViewerTuple _scroll_viewer_tup;
      readonly double _drift_multiplier;
      Point? _drag_start;
      double _x_drift, _y_drift;

      public static readonly DependencyProperty FillProperty =
         WPFHelper.create_dp<SolidColorBrush, DragRectAdorner>(
            nameof(Fill),
            FrameworkPropertyMetadataOptions.AffectsRender,
            (o, v) => o._fill = v,
            FrozenBrushCache.get_brush(DataGrid.DefaultSelectionHighlight));

      public SolidColorBrush Fill {
         get => GetValue(FillProperty) as SolidColorBrush;
         set => SetValue(FillProperty, value);
      }

      public static readonly DependencyProperty StrokeProperty =
         WPFHelper.create_dp<Pen, DragRectAdorner>(
            nameof(Stroke),
            FrameworkPropertyMetadataOptions.AffectsRender,
            (o, v) => o._stroke = v,
            FrozenPenCache.get_pen(DataGrid.DefaultSelectionHighlight));

      public Pen Stroke {
         get => GetValue(StrokeProperty) as Pen;
         set => SetValue(StrokeProperty, value);
      }

      /// <summary>
      /// Traces mouse-drag motion with a rectangluar region.
      /// </summary>
      /// <param name="adorned_element">The element to bind the adorner to.</param>
      /// <param name="fill">The rectangular region's fill.</param>
      /// <param name="stroke">The rectangular region's stroke.</param>
      /// <param name="scroll_viewer">The element's ScrollViewer.</param>
      /// <param name="drift_multiplier">The number of pixels per scroll unit.</param>
      public DragRectAdorner(FrameworkElement adorned_element, ScrollViewer scroll_viewer = null, double drift_multiplier = 1) : base(adorned_element) {
         _drift_multiplier = drift_multiplier;
         AdornedElement.PreviewMouseDown += _on_preview_mouse_down;
         AdornedElement.PreviewMouseMove += _on_preview_mouse_move;
         AdornedElement.PreviewMouseUp += _on_preview_mouse_up;
         if (scroll_viewer != null) {
            _scroll_viewer_tup = new ScrollViewerTuple(scroll_viewer);
            _scroll_viewer_tup.scroll_viewer.ScrollChanged += _scroll_viewer_ScrollChanged;
         }
      }

      public void Dispose() {
         AdornedElement.PreviewMouseDown -= _on_preview_mouse_down;
         AdornedElement.PreviewMouseMove -= _on_preview_mouse_move;
         AdornedElement.PreviewMouseUp -= _on_preview_mouse_up;
         if (_scroll_viewer_tup != null)
            _scroll_viewer_tup.scroll_viewer.ScrollChanged -= _scroll_viewer_ScrollChanged;
      }

      protected override void OnRender(DrawingContext drawingContext) {
         if (_drag_start.HasValue) {
            Point adj_start = _within_bounds(_drag_start.Value.X + _x_drift * _drift_multiplier, _drag_start.Value.Y + _y_drift * _drift_multiplier);
            Point adj_stop = _within_bounds(Mouse.GetPosition(this));
            Rect rect = new Rect(adj_start, adj_stop);
            drawingContext.DrawRectangle(_fill, _stroke, rect);
         }
      }

      static Point _within_bounds(Point pt) => _within_bounds(pt.X, pt.Y);
      static Point _within_bounds(double x, double y) => new Point(Math.Max(0, x), Math.Max(0, y));

      void _destroy_drag() {
         _drag_start = null;
         _x_drift = 0;
         _y_drift = 0;
      }

      void _on_preview_mouse_down(object sender, MouseButtonEventArgs e) {
         if (_scroll_viewer_tup == null || (_scroll_viewer_tup.presenter != null && _scroll_viewer_tup.presenter.IsMouseOver))
            _drag_start = e.GetPosition(this);
      }

      void _on_preview_mouse_move(object sender, MouseEventArgs e) {
         if (_drag_start.HasValue) {
            if (e.MouseDevice.LeftButton != MouseButtonState.Pressed)
               _destroy_drag();
            InvalidateVisual();
         }
      }

      void _on_preview_mouse_up(object sender, MouseButtonEventArgs e) {
         _destroy_drag();
         InvalidateVisual();
      }

      void _scroll_viewer_ScrollChanged(object sender, ScrollChangedEventArgs e) {
         if (_drag_start.HasValue) {
            _x_drift -= e.HorizontalChange;
            _y_drift -= e.VerticalChange;
            InvalidateVisual();
         }
      }

      class ScrollViewerTuple {
         public readonly ScrollViewer scroll_viewer;
         ScrollContentPresenter _presenter;

         public ScrollViewerTuple(ScrollViewer scroll_viewer) =>
            this.scroll_viewer = scroll_viewer;

         public ScrollContentPresenter presenter => _presenter ?? (_presenter = scroll_viewer.descendants_of_type<ScrollContentPresenter>().FirstOrDefault());
      }
   }
}