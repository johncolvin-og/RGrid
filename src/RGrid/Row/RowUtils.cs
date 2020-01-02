using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;

namespace RGrid.Controls {
   static class RowUtils {
      public const int NO_CELL = -1;

      public static ColumnGeometry? column_at(IReadOnlyList<ColumnGeometry> column_geometry, Point p) {
         foreach (var col in column_geometry) {
            if (p.X >= col.left && p.X <= col.right) {
               return col;
            }
         }
         return null;
      }

      public static ColumnGeometry? column_at(IReadOnlyList<ColumnGeometry> column_geometry, int index) {
         if (index >= 0 && index < column_geometry.Count) {
            return column_geometry[index];
         }
         return null;
      }

      public static int column_index(IReadOnlyList<ColumnGeometry> column_geometry, ColumnGeometry column) {
         for (var x = 0; x < column_geometry.Count; ++x) {
            if (column_geometry[x].column == column.column) {
               return x;
            }
         }
         return NO_CELL;
      }

      public static Size measure(ListBoxItem container, IReadOnlyCollection<ColumnGeometry> column_geometry, Size available_size, double row_height) {
         if (container != null) {
            double cw = container.ActualWidth;
            if (!double.IsNaN(cw) && cw <= available_size.Width)
               return new Size(cw, row_height);
         }
         double col_width_sum = 0;
         foreach (var cg in column_geometry) {
            col_width_sum += cg.width;
            if (col_width_sum > available_size.Width) {
               col_width_sum = available_size.Width;
               break;
            }
         }
         return new Size(col_width_sum, row_height);
      }

      /// <summary>
      /// Scrolls the given column into view.
      /// Returns the adjusted ColumnGeometry for the new column position
      /// <para/>Note: the new column geometry has been pre-computed and the grid layout pass has probably not yet happened
      /// </summary>
      public static ColumnGeometry scroll_into_view(DataGrid grid, ColumnGeometry col) {
         if (col.is_partially_obscured) {
            var obscured_width = col.left - col.unclipped_left;
            grid.scroll_horizontal(-obscured_width);
            return new ColumnGeometry(
               col.column, col.left, col.left, col.right);
         }
         return col;
      }

      public static FrameworkElement position_control_over(FrameworkElement row_view, DataGrid grid, FrameworkElement control, ColumnGeometry column) {
         Point actual_left = row_view.TranslatePoint(new Point(column.unclipped_left, 0), grid);
         // Use the margin to position the control
         control.Margin = new Thickness(actual_left.X + 1, actual_left.Y + 1, 0, 0);
         control.Width = Math.Max(column.width - 2, 0);
         control.Height = row_view.Height;
         if (column.is_partially_obscured) {
            var clipped_width = column.left - column.unclipped_left;
            // Rect is in the coordinate space of the edit control
            control.Clip = new RectangleGeometry(new Rect(clipped_width, 0, column.width - clipped_width, row_view.Height));
         } else {
            control.Clip = null;
         }
         control.Visibility = Visibility.Visible;
         control.Focus();
         return control;
      }
   }
}