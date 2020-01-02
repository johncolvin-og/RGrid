using bts;
using bts.utils;
using Disposable.Extensions;
using Disposable.Extensions.Utilities;
using RGrid.Utility;
using System;
using System.Windows;
using System.Windows.Controls.Primitives;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;

namespace RGrid.WPF {
   internal enum HorizontalPosition { Left, Middle, Right }
   internal enum VerticalPosition { Top, Middle, Bottom }

   class PopupAdorner : Adorner {
      bool _enable_drag_move;
      IDisposable _drag_move_hook, _auto_close_hook;

      public PopupAdorner(UIElement adornedElement, FrameworkElement popupTarget)
         : this(adornedElement, popupTarget, null, HorizontalPosition.Middle, VerticalPosition.Bottom) { }

      public PopupAdorner(UIElement adornedElement, FrameworkElement popupTarget, FrameworkElement popupElement, HorizontalPosition horizontalPosition, VerticalPosition verticalPosition)
         : this(adornedElement, popupTarget, popupElement, horizontalPosition, verticalPosition, 0.0, 0.0) { }

      public PopupAdorner(UIElement adornedElement, FrameworkElement popupTarget, FrameworkElement popupElement, HorizontalPosition horizontalPosition, VerticalPosition verticalPosition, double horizontalOffset, double verticalOffset)
         : base (adornedElement) {
         _popupTarget = popupTarget;
         _popupElement = popupElement;
         _horizontalPosition = horizontalPosition;
         _verticalPosition = verticalPosition;
         _horizontalOffset = horizontalOffset;
         _verticalOffset = verticalOffset;
      }

      private readonly FrameworkElement _popupTarget, _popupElement;
      private HorizontalPosition _horizontalPosition;
      private VerticalPosition _verticalPosition;
      private double _horizontalOffset = 0, _verticalOffset = 0;
      private bool _suspended;
      private int _visualChildrenCount = 0;

      public bool Suspended { get { return _suspended; } set { if (_suspended = value) InvalidateVisual(); } }
      public HorizontalPosition HorizontalPosition { get { return _horizontalPosition; } set { _horizontalPosition = value; _internalInvalidateArrange(); } }
      public VerticalPosition VerticalPosition { get { return _verticalPosition; } set { _verticalPosition = value; _internalInvalidateArrange(); } }
      public double HorizontalOffset { get { return _horizontalOffset; } set { _horizontalOffset = value; _internalInvalidateArrange(); } }
      public double VerticalOffset { get { return _verticalOffset; } set { _verticalOffset = value; _internalInvalidateArrange(); } }
      public FrameworkElement PopupElement { get { return _popupElement; } }
      public FrameworkElement PopupTarget { get { return _popupTarget; } }

      private void _internalInvalidateArrange() { if (!_suspended) InvalidateArrange(); }

      #region IsOpen

      public static readonly DependencyProperty IsOpenProperty = DependencyProperty.Register("IsOpen", typeof(bool), typeof(PopupAdorner), new PropertyMetadata(false, OnIsOpenChanged));
      private static void OnIsOpenChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) {
         var target = (PopupAdorner)d;
         if ((bool)e.NewValue) {
            target._visualChildrenCount = 1;
            target.AddVisualChild(target._popupElement);
            target.InvalidateVisual();
            target.RaiseEvent(new RoutedEventArgs(OpenedEvent, target));
         } else {
            target._visualChildrenCount = 0;
            target.RemoveVisualChild(target._popupElement);
            target.InvalidateVisual();
            target.RaiseEvent(new RoutedEventArgs(ClosedEvent, target));
         }
         target._reset_drag_move_behavior();
      }
      public bool IsOpen { get { return (bool)GetValue(IsOpenProperty); } set { SetValue(IsOpenProperty, value); } }

      #endregion

      #region StaysOpen
      public static readonly DependencyProperty StaysOpenProperty =
         Popup.StaysOpenProperty.AddOwner(typeof(PopupAdorner),
            new FrameworkPropertyMetadata(
               true,
               (d, e) => {
                  var popa = ExceptionAssert.Argument.Is<PopupAdorner>(d, nameof(d));
                  DisposableUtils.Dispose(ref popa._auto_close_hook);
                  if (!(bool)e.NewValue) {
                     FocusManager.SetIsFocusScope(popa.AdornedElement, true);
                     popa.AdornedElement.Focus();
                     popa.AdornedElement.PreviewGotKeyboardFocus += on_focus_changed;
                     popa.AdornedElement.GotFocus += on_focus_changed;
                     popa._auto_close_hook = DisposableFactory.Create(() => {
                        popa.AdornedElement.PreviewGotKeyboardFocus -= on_focus_changed;
                        popa.AdornedElement.GotFocus -= on_focus_changed;
                     });
                     // callbacks
                     void on_focus_changed(object sender, RoutedEventArgs _e) {
                        if (!popa.PopupElement.IsKeyboardFocusWithin && FocusManager.GetFocusedElement(popa.PopupElement) == null)
                           popa.SetCurrentValue(IsOpenProperty, false);
                     };
                  }
               }));

      public bool StaysOpen {
         get => (bool)GetValue(StaysOpenProperty);
         set => SetValue(StaysOpenProperty, value);
      }
      #endregion

      #region Opened Event

      public static readonly RoutedEvent OpenedEvent = EventManager.RegisterRoutedEvent("Opened", RoutingStrategy.Direct, typeof(RoutedEventHandler), typeof(PopupAdorner));
      public event RoutedEventHandler Opened { add { AddHandler(OpenedEvent, value); } remove { RemoveHandler(OpenedEvent, value); } }

      #endregion

      #region Closed Event

      public static readonly RoutedEvent ClosedEvent = EventManager.RegisterRoutedEvent("Closed", RoutingStrategy.Direct, typeof(RoutedEventHandler), typeof(PopupAdorner));
      public event RoutedEventHandler Closed { add { AddHandler(ClosedEvent, value); } remove { RemoveHandler(ClosedEvent, value); } }

      #endregion

      public bool OpenRelativeToMouse { get; set; }
      public bool EnableDragMove {
         get => _enable_drag_move;
         set {
            if (value != _enable_drag_move) {
               _enable_drag_move = value;
               _reset_drag_move_behavior();
            }
         }
      }

      protected override int VisualChildrenCount { get { return _visualChildrenCount; } }

      protected override Visual GetVisualChild(int index) {
         if (index != 0) throw new ArgumentOutOfRangeException();
         else if (!IsOpen) throw new InvalidOperationException();
         return _popupElement;
      }

      protected override Size MeasureOverride(Size constraint) {
         _popupElement.Measure(constraint);
         return _popupElement.DesiredSize;
      }

      protected override Size ArrangeOverride(Size finalSize) {
         _popupElement.Arrange(new Rect(_get_desired_tl(), finalSize));
         return _popupElement.DesiredSize;
      }

      Point _get_desired_tl() {
         if (_popupElement.IsLoaded)
            return _popupElement.TranslatePoint(new Point(0, 0), this);
         double el_width = _popupElement.DesiredSize.Width;
         double el_height = _popupElement.DesiredSize.Height;
         Point tl;
         if (OpenRelativeToMouse) {
            tl = Mouse.GetPosition(this);
            double rltv_x, rltv_y;
            switch (_horizontalPosition) {
               case HorizontalPosition.Left: rltv_x = -el_width; break;
               case HorizontalPosition.Middle: rltv_x = -(0.5 * el_width); break;
               case HorizontalPosition.Right: rltv_x = el_width; break;
               default: throw new NotImplementedException();
            }
            switch (_verticalPosition) {
               case VerticalPosition.Top: rltv_y = -el_height; break;
               case VerticalPosition.Middle: rltv_y = -(0.5 * el_height); break;
               case VerticalPosition.Bottom: rltv_y = 0; break;
               default: throw new NotImplementedException();
            }
            tl = new Point(tl.X + rltv_x + _horizontalOffset, tl.Y + rltv_y + _verticalOffset);
         } else {
            tl = _popupTarget.TranslatePoint(new Point(0, 0), AdornedElement);
            double rltv_x, rltv_y;
            switch (_horizontalPosition) {
               case HorizontalPosition.Left: rltv_x = -el_width; break;
               case HorizontalPosition.Middle: rltv_x = 0.5 * _popupTarget.ActualWidth; break;
               case HorizontalPosition.Right: rltv_x = _popupTarget.ActualWidth; break;
               default: throw new NotImplementedException();
            }
            switch (_verticalPosition) {
               case VerticalPosition.Top: rltv_y = -el_height; break;
               case VerticalPosition.Middle: rltv_y = 0.5 * _popupTarget.ActualHeight; break;
               case VerticalPosition.Bottom: rltv_y = _popupTarget.ActualHeight; break;
               default: throw new NotImplementedException();
            }
            tl = new Point(tl.X + rltv_x + _horizontalOffset, tl.Y + rltv_y + _verticalOffset);
         }
         if (ClipToBounds) {
            tl = tl.within_bounds(
               x_max: AdornedElement.RenderSize.Width - el_width,
               y_max: AdornedElement.RenderSize.Height - el_height);
         }
         return _popupTarget.TranslatePoint(tl, _popupTarget);
      }

      void _reset_drag_move_behavior() {
         DisposableUtils.Dispose(ref _drag_move_hook);
         if (_enable_drag_move && IsOpen) {
            Point? drag_origin = null;
            _drag_move_hook = DragDropHelper.subscribe_drag_coordinates(this).Subscribe(delta => {
               if (delta.HasValue) {
                  var d = delta.Value;
                  var rs = AdornedElement.RenderSize;
                  // ensure within bounds of AdornedElement
                  var relative_pos = Mouse.GetPosition(AdornedElement);
                  if (relative_pos.X < 0)
                     d.X -= relative_pos.X;
                  else if (relative_pos.X > rs.Width)
                     d.X -= (relative_pos.X - rs.Width);
                  if (relative_pos.Y < 0)
                     d.Y -= relative_pos.Y;
                  else if (relative_pos.Y > rs.Height)
                     d.Y -= (relative_pos.Y - rs.Height);
                  //
                  if (drag_origin.HasValue) {
                     var m = Margin;
                     m.Left += d.X - drag_origin.Value.X;
                     m.Top += d.Y - drag_origin.Value.Y; 
                     Margin = m;
                  } else {
                     drag_origin = d;
                     Mouse.Capture(this);
                  }
               } else {
                  drag_origin = null;
                  ReleaseMouseCapture();
               }
            });
         }
      }
   }
}