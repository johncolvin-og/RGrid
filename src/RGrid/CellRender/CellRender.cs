using Disposable.Extensions;
using RGrid.Utility;
using RGrid.WPF;
using System;
using System.Linq;
using System.Windows;
using System.Windows.Media;

namespace RGrid {
   public partial class DataGrid {
      public static class CellRender {
         public static Rect? get_clip(Size content_size, double row_height, double left, double unclipped_left, double width) =>
            content_size == null ? get_clip(0, 0, row_height, left, unclipped_left, width) :
               get_clip(content_size.Width, content_size.Height, row_height, left, unclipped_left, width);

         public static Rect? get_clip(double content_width, double content_height, double row_height, double left, double unclipped_left, double width) {
            if (content_width > width || unclipped_left < left)
               return new Rect(left, 0, width - left + unclipped_left, row_height);
            return null;
         }

         public static IDisposable draw_with_clip(DrawingContext dc, double raw_content_width, double raw_content_height, Thickness padding, double row_height, ColumnGeometry position) =>
            draw_with_clip(dc,
               raw_content_width + padding.Left + padding.Right,
               raw_content_height + padding.Top + padding.Bottom,
               row_height, position.left, position.unclipped_left, position.width);

         public static IDisposable draw_with_clip(DrawingContext dc, double content_width, double content_height, double row_height, double left, double unclipped_left, double width) {
            var clip = get_clip(content_width, content_height, row_height, left, unclipped_left, width);
            if (clip.HasValue) {
               dc.PushClip(new RectangleGeometry(clip.Value));
               return DisposableFactory.Create(dc.Pop);
            } else return DisposableFactory.Create(() => { });
         }

         public static void draw_text(
            DrawingContext dc, GlyphContext glyph_context, double font_size, string text, ColumnGeometry position, double row_height,
            Brush foreground, Brush background, HorizontalAlignment x_align, VerticalAlignment y_align, Thickness padding) {
            //
            if (!string.IsNullOrEmpty(text)) {
               var gr = glyph_context.get_glyph_run(text, font_size, position.unclipped_left, 0, position.right, row_height, x_align, y_align, padding);
               draw_text(dc, gr, gr.AdvanceWidths.Sum(), font_size, position, padding, row_height, foreground, background);
            }
         }

         public static void draw_text(DrawingContext dc, GlyphRun glyph_run, double glyph_width, double font_size, ColumnGeometry position, Thickness padding, double row_height, Brush foreground, Brush background) {
            using (draw_with_clip(dc, glyph_width, font_size, padding, row_height, position)) {
               if (background != null)
                  dc.DrawRectangle(background, null, new Rect(position.left, 0, position.width, row_height));
               dc.DrawGlyphRun(foreground, glyph_run);
            }
         }

         /// <summary>
         /// Changes the cell_view's Clip/Visibility (if necessary), with respect to the ColumnGeometry position.
         /// </summary>
         public static void change_position(FrameworkElement cell_view, double row_height, ColumnGeometry? position) {
            if (position.HasValue) {
               WPFHelper.set_if_changed(cell_view, VisibilityProperty, Visibility.Visible);
               var clip = get_clip(cell_view.DesiredSize, row_height, 0, position.Value.unclipped_left - position.Value.left, position.Value.width);
               var el_clip = cell_view.Clip as RectangleGeometry;
               if (clip.HasValue) {
                  if (el_clip == null || !el_clip.Rect.Equals(clip.Value))
                     cell_view.Clip = new RectangleGeometry(clip.Value);
               } else if (cell_view.Clip != null) {
                  cell_view.Clip = null;
               }
            } else {
               WPFHelper.set_if_changed(cell_view, VisibilityProperty, Visibility.Collapsed);
            }
         }
      }
   }
}