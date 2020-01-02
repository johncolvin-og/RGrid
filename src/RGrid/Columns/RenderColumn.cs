using RGrid.Controls;
using RGrid.Filters;
using RGrid.Utility;
using System;
using System.Windows;
using System.Windows.Media;

namespace RGrid {
   public partial class DataGrid {
      public interface IRenderColumn<TRowVM> {
         void draw(TRowVM row, DrawingContext dc, ColumnGeometry position, double row_height);
      }
      /// <summary>
      /// Represents a column that renders it's cell-visual with the row's DrawingContext.
      /// <para/> Note: designed to be used by HighPerformanceRow&lt;<typeparamref name="TRowVM"/>&gt;.
      /// </summary>
      public abstract class RenderColumnBase<TRowVM> : ColumnBase, IRenderColumn<TRowVM> {
         protected RenderColumnBase() { }

         public abstract void draw(TRowVM row, DrawingContext dc, ColumnGeometry position, double row_height);
      }

      public class RenderTextColumn<TRowVM> : RenderColumnBase<TRowVM>, IRenderColumn<TRowVM>, IFilterableColumn {
         readonly Func<TRowVM, string> _get_text;

         public RenderTextColumn(
            Func<TRowVM, string> get_text,
            IDataGridColumnFilter filter = null,
            string filter_template = null,
            GlyphContext glyph_context = null)
            : base() {
            _get_text = get_text;
            this.filter = filter;
            this.filter_template = filter_template;
            this.glyph_context = glyph_context ?? TextUtils.StandardGlyphContext;
         }

         public IDataGridColumnFilter filter { get; }
         public string filter_template { get; }
         public Brush background { get; set; }
         public VerticalAlignment vertical_alignment { get; set; } = VerticalAlignment.Center;
         public Thickness padding { get; set; } = DefaultCellPadding;
         public GlyphContext glyph_context { get; set; } = TextUtils.StandardGlyphContext;

         public override void draw(TRowVM row, DrawingContext dc, ColumnGeometry position, double row_height) =>
            CellRender.draw_text(
               dc, glyph_context, font_size, _get_text(row), position, row_height,
               font_brush, background, horizontal_alignment, vertical_alignment, padding);
      }

      public class RenderTextColumn<TRowVM, TColKey> : RenderTextColumn<TRowVM>, IKeyedColumn<TColKey> {
         public RenderTextColumn(TColKey key, Func<TRowVM, string> get_text, IDataGridColumnFilter filter, string filter_template)
           : base(get_text, filter, filter_template) => this.key = key;

         public TColKey key { get; }
      } 
   }
}