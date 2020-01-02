using RGrid.WPF;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls.Primitives;
using System.Windows.Media;

namespace RGrid {
   // Column types are nested inside the DIYGrid
   internal class SuperHeader : ToggleButton {
      static SuperHeader() {
         IsCheckedProperty.AddOwner(typeof(SuperHeader), new FrameworkPropertyMetadata(property_changed));
      }

      public SuperHeader(Typeface font, string long_text, string short_text, IEnumerable<ColumnBase> children) {
         FullHeaderText = long_text;
         CollapsedHeaderText = short_text ?? string.Join("", long_text.Where(Char.IsUpper));
         CollapsedWidth = font.MeasureText((double)GetValue(FontSizeProperty), CollapsedHeaderText).Width + 4;
         SubColumns = children.ToList();
         Content = long_text;
      }

      private static void property_changed(DependencyObject d, DependencyPropertyChangedEventArgs e) {
         var self = (SuperHeader)d;
         if (self.is_collapsed) self.collapse();
         else self.expand();
      }

      public string FullHeaderText { get; private set; }
      public string CollapsedHeaderText { get; private set; }
      public double CollapsedWidth { get; private set; }

      public List<ColumnBase> SubColumns { get; private set; }

      public bool is_collapsed { get { return IsChecked == true; } set { IsChecked = value; } }

      internal void collapse() {
         double width = CollapsedWidth / SubColumns.Count;
         foreach (var col in SubColumns) {
            col.original_width = col.ActualWidth;
            col.Width = width;
            col.is_collapsed = true;
         }
         Content = CollapsedHeaderText;
      }
      internal void expand() {
         foreach (var col in SubColumns) {
            if (!double.IsNaN(col.original_width)) {
               col.Width = col.original_width;
               col.original_width = double.NaN;
               col.is_collapsed = false;
            }
         }
         Content = FullHeaderText;
      }
   }
}