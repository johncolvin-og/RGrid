using Disposable.Extensions;
using RGrid.Utility;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace RGrid.WPF {
   public static class UIElementExtensions {
      public static bool ChildAtPoint<T>(this UIElement target, Point pt, out T result) where T : DependencyObject {
         result = target.ChildAtPoint<T>(pt);
         return result != null;
      }

      public static T ChildAtPoint<T>(this UIElement element, Point pt) where T : DependencyObject =>
         element.IsWithinBounds(pt) && element.InputHitTest(pt) is UIElement hit ?
            (hit == element ? null : hit as T ?? FindAncestor<T>(hit)) :
            null;

      public static T FindAncestor<T>(this DependencyObject dependency_object) where T : DependencyObject =>
         dependency_object == null ? null :
            dependency_object as T ?? FindAncestor<T>(VisualTreeHelper.GetParent(dependency_object));

      public static Point GetCenter(this UIElement element) =>
         new Point(element.RenderSize.Width * 0.5, element.RenderSize.Height * 0.5);

      public static Point GetChildCenter(this UIElement parent, UIElement child) =>
         child.TranslatePoint(GetCenter(child), parent);

      public static Point GetChildTopLeft(this UIElement parent, UIElement child) =>
         child.TranslatePoint(new Point(0, 0), parent);

      public static Rect GetChildBounds(this UIElement target, UIElement child) {
         var child_tl = target.GetChildTopLeft(child);
         return new Rect(child_tl.X, child_tl.Y, child.RenderSize.Width, child.RenderSize.Height);
      }

      public static bool IsWithinBounds(this UIElement target, Point pt) =>
         pt.X >= 0 && pt.X < target.RenderSize.Width && pt.Y >= 0 && pt.Y < target.RenderSize.Height;

      public static IEnumerable<DependencyObject> VisualAncestors(this DependencyObject dependency_object) {
         var parent = VisualTreeHelper.GetParent(dependency_object);
         if (parent != null) {
            yield return parent;
            foreach (var ancestor in VisualAncestors(parent))
               yield return ancestor;
         }
      }

      public static IEnumerable<T> VisualAncestors<T>(this DependencyObject dependency_object) where T : DependencyObject =>
         VisualAncestors(dependency_object).OfType<T>();

      public static IDisposable DeferInvalidateVisual(this UIElement target) =>
         DisposableFactory.Create(target.InvalidateVisual);

      public static IDisposable SubscribeMouseEnter(this UIElement target, MouseEventHandler callback) {
         target.MouseEnter += callback;
         return DisposableFactory.Create(() => target.MouseEnter -= callback);
      }

      public static IDisposable SubscribeMouseMove(this UIElement target, MouseEventHandler callback) {
         target.MouseMove += callback;
         return DisposableFactory.Create(() => target.MouseMove -= callback);
      }

      public static IDisposable SubscribeMouseLeave(this UIElement target, MouseEventHandler callback) {
         target.MouseLeave += callback;
         return DisposableFactory.Create(() => target.MouseLeave -= callback);
      }

      public static IDisposable SubscribeMouseDown(this UIElement target, MouseButtonEventHandler callback) {
         target.MouseDown += callback;
         return DisposableFactory.Create(() => target.MouseDown -= callback);
      }

      public static IDisposable SubscribeMouseUp(this UIElement target, MouseButtonEventHandler callback) {
         target.MouseUp += callback;
         return DisposableFactory.Create(() => target.MouseUp -= callback);
      }

      public static IDisposable SubscribeMouseLeftButtonDown(this UIElement target, MouseButtonEventHandler callback) {
         target.MouseLeftButtonDown += callback;
         return DisposableFactory.Create(() => target.MouseLeftButtonDown -= callback);
      }

      public static IDisposable SubscribeMouseLeftButtonUp(this UIElement target, MouseButtonEventHandler callback) {
         target.MouseLeftButtonUp += callback;
         return DisposableFactory.Create(() => target.MouseLeftButtonUp -= callback);
      }

      public static IDisposable SubscribeMouseDoubleClick(this Control target, MouseButtonEventHandler callback) {
         target.MouseDoubleClick += callback;
         return DisposableFactory.Create(() => target.MouseDoubleClick -= callback);
      }

      public static IDisposable SubscribeLocationChanged(this Window window, EventHandler callback) {
         window.LocationChanged += callback;
         return DisposableFactory.Create(() => window.LocationChanged -= callback);
      }

      public static IDisposable SubscribeIsVisibleChanged(this Window window, DependencyPropertyChangedEventHandler callback) {
         window.IsVisibleChanged += callback;
         return DisposableFactory.Create(() => window.IsVisibleChanged -= callback);
      }

      public static IDisposable SubscribeClosed(this Window window, EventHandler callback) {
         window.Closed += callback;
         return DisposableFactory.Create(() => window.Closed -= callback);
      }

      public static IDisposable SubscribeDragEnter(this UIElement target, DragEventHandler callback) {
         target.DragEnter += callback;
         return DisposableFactory.Create(() => target.DragEnter -= callback);
      }

      public static IDisposable SubscribeDragOver(this UIElement target, DragEventHandler callback) {
         target.DragOver += callback;
         return DisposableFactory.Create(() => target.DragOver -= callback);
      }

      public static IDisposable SubscribeDragLeave(this UIElement target, DragEventHandler callback) {
         target.DragLeave += callback;
         return DisposableFactory.Create(() => target.DragLeave -= callback);
      }

      public static IDisposable SubscribePreviewMouseDown(this UIElement target, MouseButtonEventHandler callback) {
         target.PreviewMouseDown += callback;
         return DisposableFactory.Create(() => target.PreviewMouseDown -= callback);
      }

      public static IDisposable SubscribePreviewDragOver(this UIElement target, DragEventHandler callback) {
         target.PreviewDragOver += callback;
         return DisposableFactory.Create(() => target.PreviewDragOver -= callback);
      }

      public static IDisposable SubscribePreviewDragLeave(this UIElement target, DragEventHandler callback) {
         target.PreviewDragLeave += callback;
         return DisposableFactory.Create(() => target.PreviewDragLeave -= callback);
      }

      public static IDisposable SubscribePreviewDrop(this UIElement target, DragEventHandler callback) {
         target.PreviewDrop += callback;
         return DisposableFactory.Create(() => target.PreviewDrop -= callback);
      }

      public static IDisposable SubscribePreviewMouseMove(this Control target, MouseEventHandler callback) {
         target.PreviewMouseMove += callback;
         return DisposableFactory.Create(() => target.PreviewMouseMove -= callback);
      }

      public static IDisposable SubscribePreviewTextInput(this UIElement target, TextCompositionEventHandler callback) {
         target.PreviewTextInput += callback;
         return DisposableFactory.Create(() => target.PreviewTextInput -= callback);
      }
   }

   public static class SelectorExtensions {
      public static IDisposable SubscribeSelectionChanged(this System.Windows.Controls.Primitives.Selector target, SelectionChangedEventHandler callback) {
         target.SelectionChanged += callback;
         return DisposableFactory.Create(() => target.SelectionChanged -= callback);
      }
   }

   public static class ScrollViewerExtensions {
      public static IDisposable SubscribeScrollChanged(this ScrollViewer target, ScrollChangedEventHandler callback) {
         target.ScrollChanged += callback;
         return DisposableFactory.Create(() => target.ScrollChanged -= callback);
      }
   }

   public static class TextBoxExtensions {
      public static string PeekText(this TextBox self, TextCompositionEventArgs e) =>
         TextUtils.InsertText(self.Text, e.Text, self.CaretIndex, self.SelectionStart, self.SelectionLength);
   }

   static class ImageSourceExtensions {
      public static Size measure_scalar(this ImageSource image_source, double max_width, double max_height) {
         if (dimension_valid(image_source.Width, max_width)) {
            if (dimension_valid(image_source.Height, max_height)) {
               // scale closest
               double x_scale = max_width / image_source.Width;
               double y_scale = max_height / image_source.Height;
               return x_scale > y_scale ?
                  new Size(image_source.Width * y_scale, max_height) :
                  new Size(max_width, image_source.Height * x_scale);
            } else {
               // scale x
               double x_scale = max_width / image_source.Width;
               return new Size(max_width, image_source.Height * x_scale);
            }
         } else if (dimension_valid(image_source.Height, max_height)) {
            // scale y
            double y_scale = max_height / image_source.Height;
            return new Size(image_source.Width * y_scale, max_height);
         } else {
            throw new InvalidOperationException($"Unable to scale image of dimensions {image_source.Width}x{image_source.Height} with {nameof(max_width)} ({max_width}) and {nameof(max_height)} ({max_height}).");
         }

         bool dimension_valid(double curr, double max) =>
            !DoubleHelper.IsNaNOrInfinityOr(curr, 0) && !DoubleHelper.IsNaNOrInfinityOr(max, 0);
      }
   }
}