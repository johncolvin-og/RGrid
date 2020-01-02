using Disposable.Extensions.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;

namespace RGrid.WPF {
   class RowSelectionAdorner : Adorner, IDisposable {
      Brush _fill;
      Pen _stroke;
      ListBox _diy_rp;
      ScrollViewer _diy_scroll;
      List<IDisposable> _disposables = new List<IDisposable>();

      #region Fill
      public static readonly DependencyProperty FillProperty =
         WPFHelper.create_dp<Brush, RowSelectionAdorner>(
            nameof(Fill),
            FrameworkPropertyMetadataOptions.AffectsRender,
            (o, v) => o._fill = v,
            FrozenBrushCache.get_brush(DataGrid.DefaultSelectionHighlight));

      public SolidColorBrush Fill {
         get => GetValue(FillProperty) as SolidColorBrush;
         set => SetValue(FillProperty, value);
      }
      #endregion

      #region Stroke
      public static readonly DependencyProperty StrokeProperty =
         WPFHelper.create_dp<Pen, RowSelectionAdorner>(
            nameof(Stroke),
            FrameworkPropertyMetadataOptions.AffectsRender,
            (o, v) => o._stroke = v,
            FrozenPenCache.get_pen(DataGrid.DefaultSelectionHighlight.with(255)));

      public Pen Stroke {
         get => GetValue(StrokeProperty) as Pen;
         set => SetValue(StrokeProperty, value);
      }
      #endregion

      public RowSelectionAdorner(ListBox list_box, ScrollViewer scroll_viewer)
         : this(list_box, scroll_viewer, list_box.descendants_of_type<ScrollContentPresenter>().FirstOrDefault()) { }

      /// <summary>
      /// Create an instance of the <see cref="RowSelectionAdorner"/> class.
      /// </summary>
      /// <param name="list_box"></param>
      /// <param name="scroll_viewer"></param>
      /// <param name="scroll_presenter">
      /// The actual AdornedElement (the ScrollViewer may clip the ScrollContentPresenter, and the most prudent way to reflect such a clip is to use it as the actual AdornedElement).
      /// </param>
      public RowSelectionAdorner(ListBox list_box, ScrollViewer scroll_viewer, ScrollContentPresenter scroll_presenter)
         : base(scroll_presenter) {
         _fill = Fill;
         _stroke = Stroke;
         // we want our clicks to go through to the underlying grid
         IsHitTestVisible = false;
         // reflect the AdornedElement's clip
         IsClipEnabled = true;

         _diy_rp = list_box;
         _diy_scroll = scroll_viewer;

         // We actually don't need mouse up/down or key up/down when we can just respond to selection changes
         _disposables.Add(((System.Windows.Controls.Primitives.Selector)_diy_rp).SubscribeSelectionChanged((s, e) => InvalidateVisual()));
         // so that we update the highlight as the grid scrolls
         _disposables.Add(_diy_scroll.SubscribeScrollChanged((s, e) => InvalidateVisual()));
      }

      protected override void OnRender(DrawingContext drawingContext) {
         base.OnRender(drawingContext);
         if (_diy_rp.SelectedItems != null && _diy_rp.SelectedItems.Count > 0) {
            foreach (var lbi in _diy_rp.SelectedItems) {
               if (_diy_rp.ItemContainerGenerator.ContainerFromItem(lbi) is FrameworkElement row && row.IsVisible) {
                  var top_left = _diy_rp.GetChildTopLeft(row);
                  var top_is_within_bounds = top_left.Y >= 0;

                  // Note that the top of the row can still technically be visible but we wont show
                  // the adorner rectangle b/c the bottom of the row will be obscured by the scrollbar
                  var bottom_is_within_bounds = _diy_rp.GetChildBounds(row).BottomRight.Y <= _diy_scroll.ActualHeight;
                  if (top_is_within_bounds && bottom_is_within_bounds) {
                     // we use the scroll content presenter in the X point calculation instead of the row
                     // because of the OB which has a right side panel that takes up some space over
                     // row width
                     drawingContext.DrawRectangle(_fill,
                        _stroke,
                        new Rect(top_left, new Point(top_left.X + AdornedElement.RenderSize.Width, top_left.Y + row.ActualHeight)));
                  }
               }
            }
         }
      }

      public void on_selection_fill(SolidColorBrush selection_fill) {
         Fill = selection_fill;
         Stroke = new Pen(selection_fill.Color.to_solid_brush(), 1);
         InvalidateVisual();
      }

      public void Dispose() =>
         DisposableUtils.DisposeItems(_disposables);
   }
}
