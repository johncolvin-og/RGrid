using EqualityComparer.Extensions;
using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;

namespace RGrid {
   // Column types are nested inside the DIYGrid
   public readonly struct CellGeometry {
      public CellGeometry(ColumnGeometry column, ListBoxItem row) {
         this.column = column;
         this.row = row;
      }

      public ColumnGeometry column { get; }
      public ListBoxItem row { get; }

      public void Deconstruct(out ListBoxItem row, out ColumnGeometry column) {
         row = this.row;
         column = this.column;
      }

      public Rect to_rect(double y) =>
         column.to_visible_rect(row.ActualHeight, y);

      public static implicit operator (ListBoxItem, ColumnGeometry)(CellGeometry cell) =>
         (cell.row, cell.column);
   }

   public readonly struct ColumnGeometry : IComparable<ColumnGeometry> {
      public static readonly IEqualityComparer<ColumnGeometry> PositionEqualityComparer =
         EqualityComparerFactory.Create<ColumnGeometry, (double, double)>(c => (c.left, c.right));

      public readonly ColumnBase column;
      public readonly double
         left,
         // If the column was unclipped
         unclipped_left,
         right;

      public ColumnGeometry(ColumnBase column, double left, double right)
         : this(column, left, left, right) { }

      public ColumnGeometry(ColumnBase column, double left, double unclipped_left, double right) {
         this.column = column;
         this.left = left;
         this.unclipped_left = unclipped_left;
         this.right = right;
      }

      public double width => right - left;
      public bool is_partially_obscured { get { return unclipped_left < left; } }
      public double visible_width => Math.Max(0, width - left + unclipped_left);

      public Rect to_rect(double row_height) =>
         new Rect(left, 0, width, row_height);

      public Rect to_visible_rect(double row_height, double y) =>
         new Rect(unclipped_left, y, visible_width, row_height);

      public override string ToString() =>
         string.Format("{3} {0:0.0},{1:0.0} w{2:0.0}", left, right, width, column.Header);

      public int CompareTo(ColumnGeometry other) {
         int comp = left.CompareTo(other.left);
         if (comp != 0)
            return comp;
         return right.CompareTo(other.right);
      }
   }
}
