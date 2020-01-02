using Disposable.Extensions;
using Disposable.Extensions.Utilities;
using RGrid.Utility;
using System;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Media;

namespace RGrid.WPF {
   enum RelativePosition { Left, Right, Above, Below }

   static class AdornerHelper {
      public static void attach(this Adorner adorner) {
         var al = AdornerLayer.GetAdornerLayer(adorner.AdornedElement);
         if (al != null) al.Add(adorner);
      }

      public static void detach(this Adorner adorner) {
         var al = AdornerLayer.GetAdornerLayer(adorner.AdornedElement);
         if (al != null) al.Remove(adorner);
      }

      public static void draw_border(this Adorner adorner, DrawingContext drawing_context, FrameworkElement element, Brush brush, Pen stroke) {
         var tl = element.TranslatePoint(new Point(0, 0), adorner);
         drawing_context.DrawRectangle(brush, stroke, new Rect(tl, new Point(tl.X + element.ActualWidth, tl.Y + element.ActualHeight)));
      }

      public static void draw_relative_text(this Adorner adorner, RelativePosition relative_position, string text,
         DrawingContext drawing_context, FrameworkElement element, Brush foreground, double x_offset, double y_offset) =>
         draw_relative_text(adorner, relative_position, TextUtils.GetFormattedText(text, FontSizes.Regular, foreground), drawing_context, element, foreground, x_offset, y_offset);

      public static void draw_relative_text(this Adorner adorner, RelativePosition relative_position, FormattedText formatted_text,
         DrawingContext drawing_context, FrameworkElement element, Brush foreground, double x_offset, double y_offset) {
         switch (relative_position) {
            case RelativePosition.Left: {
                  var tl = element.TranslatePoint(new Point(0, 0), adorner);
                  drawing_context.DrawText(formatted_text, new Point(tl.X - formatted_text.Width + x_offset, tl.Y + y_offset));
               } break;
            case RelativePosition.Above: {
                  var tl = element.TranslatePoint(new Point(0, 0), adorner);
                  drawing_context.DrawText(formatted_text, new Point(tl.X + x_offset, tl.Y - formatted_text.Height + y_offset));
               } break;
            case RelativePosition.Below: {
                  var bl = element.TranslatePoint(new Point(0, element.ActualHeight), adorner);
                  drawing_context.DrawText(formatted_text, new Point(bl.X + x_offset, bl.Y + y_offset));
               } break;
            default: throw new NotImplementedException();
         }
      }

      public static IDisposable attach_continuously_on_loaded(Func<Adorner> factory, FrameworkElement element) {
         IDisposable dispose = null;
         if (element.IsLoaded)
            attach_new();
         element.Loaded += on_loaded;
         element.Unloaded += on_unloaded;
         return DisposableFactory.Create(() => {
            DisposableUtils.Dispose(ref dispose);
            element.Loaded -= on_loaded;
            element.Unloaded -= on_unloaded;
         });
         void on_loaded(object sender, RoutedEventArgs e) => attach_new();
         void on_unloaded(object sender, RoutedEventArgs e) => DisposableUtils.Dispose(ref dispose);
         void attach_new() {
            DisposableUtils.Dispose(ref dispose);
            var adorner = factory();
            adorner.attach();
            dispose = DisposableFactory.Create(() => {
               adorner.detach();
               (adorner as IDisposable)?.Dispose();
               adorner = null;
            });
         }
      }

   }
}
