using Disposable.Extensions;
using RGrid.Utility;
using System;
using System.Collections.Generic;
using System.Reactive.Linq;
using System.Text;
using System.Windows;
using System.Windows.Input;

namespace RGrid.WPF {
   static class DragDropHelper {
      public static bool is_sufficient_delta(Point origin, Point current) =>
         origin.X != -1 && origin.Y != -1 &&
            (Math.Abs(current.X - origin.X) >= SystemParameters.MinimumHorizontalDragDistance ||
            Math.Abs(current.Y - origin.Y) >= SystemParameters.MinimumVerticalDragDistance);

      public static IObservable<Point?> subscribe_drag_coordinates(
         FrameworkElement element,
         FrameworkElement init_element = null,
         Rect? drag_start_bounds = null,
         bool use_preview_events = false,
         MouseButton mouse_button = MouseButton.Left) {
         if (element == null)
            throw new ArgumentNullException(nameof(element));
         if (init_element == null)
            init_element = element;
         return Observable.Create<Point?>(o => {
            Point? mouse_down = null, drag_start = null;
            if (use_preview_events) {
               init_element.PreviewMouseDown += on_mouse_down;
               element.PreviewMouseMove += on_mouse_move;
               element.PreviewMouseUp += on_mouse_up;
            } else {
               init_element.MouseDown += on_mouse_down;
               element.MouseMove += on_mouse_move;
               element.MouseUp += on_mouse_up;
            }
            return DisposableFactory.Create(() => {
               if (use_preview_events) {
                  init_element.PreviewMouseDown -= on_mouse_down;
                  element.PreviewMouseMove -= on_mouse_move;
                  element.PreviewMouseUp -= on_mouse_up;
               } else {
                  init_element.MouseDown -= on_mouse_down;
                  element.MouseMove -= on_mouse_move;
                  element.MouseUp -= on_mouse_up;
               }
            });
            // callbacks
            void on_mouse_up(object sender, MouseButtonEventArgs e) {
               mouse_down = null;
               if (drag_start.HasValue)
                  o.OnNext(drag_start = null);
            }
            void on_mouse_move(object sender, MouseEventArgs e) {
               if (drag_start.HasValue) {
                  o.OnNext(MouseHelper.button_state(mouse_button) == MouseButtonState.Pressed ? e.GetPosition(element) : (drag_start = null));
                  e.Handled = true;
               } else if (mouse_down.HasValue) {
                  var curr_pt = e.GetPosition(element);
                  if (is_sufficient_delta(mouse_down.Value, curr_pt)) {
                     e.Handled = true;
                     drag_start = curr_pt;
                     mouse_down = null;
                     o.OnNext(curr_pt);
                  }
               }
            }
            void on_mouse_down(object sender, MouseButtonEventArgs e) {
               var pt = e.GetPosition(init_element);
               if (!drag_start_bounds.HasValue || drag_start_bounds.Value.Contains(pt)) {
                  drag_start = null;
                  mouse_down = pt;
               }
            }
         });
      }
   }
}
