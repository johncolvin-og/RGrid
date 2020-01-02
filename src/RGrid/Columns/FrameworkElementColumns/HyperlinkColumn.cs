using RGrid.Controls;
using RGrid.Filters;
using System;
using System.Windows;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;

namespace RGrid {
   partial class DataGrid {
      internal class HyperlinkElementColumn<TKey> : ColumnBase<TKey>, IFrameworkElementColumn, IFilterableColumn, IKeyedColumn<TKey> {
         public HyperlinkElementColumn(TKey key, IDataGridColumnFilter filter, string filter_template)
            : base(key) {
            this.filter = filter;
            this.filter_template = filter_template;
         }

         public event Action<HyperlinkCell> view_generated;

         public Style ElementStyle { get; set; }
         public IDataGridColumnFilter filter { get; set; }
         public string filter_template { get; set; }
         // bindings
         public BindingBase CommandBinding { get; set; }
         public BindingBase CommandParameterBinding { get; set; }
         public BindingBase TextBinding { get; set; }

         public FrameworkElement view_factory() {
            var hl = new HyperlinkCell();
            hl.SetCurrentValue(DataGrid.ColumnProperty, this);
            // bindings
            if (TextBinding != null)
               hl.SetBinding(HyperlinkCell.TextProperty, TextBinding);
            if (CommandBinding != null)
               hl.SetBinding(HyperlinkCell.CommandProperty, CommandBinding);
            if (CommandParameterBinding != null)
               hl.SetBinding(HyperlinkCell.CommandParameterProperty, CommandParameterBinding);
            hl.SetBinding(HyperlinkCell.ForegroundProperty, new Binding(nameof(font_brush)) { Source = this });
            hl.SetBinding(HyperlinkCell.FontFamilyProperty, new Binding(nameof(font_family)) { Source = this });
            hl.SetBinding(HyperlinkCell.FontSizeProperty, new Binding(nameof(font_size)) { Source = this });
            hl.SetBinding(HyperlinkCell.HorizontalContentAlignmentProperty, new Binding(nameof(horizontal_alignment)) { Source = this });
            hl.SetBinding(FrameworkElement.WidthProperty, new Binding(nameof(ActualWidth)) { Source = this });
            hl.SetBinding(
               CursorProperty,
               new Binding {
                  Source = hl,
                  Path = new PropertyPath(HyperlinkCell.IsMouseOverClickAreaProperty),
                  Converter = new BooleanToObjectConverter {
                     ValueForFalse = Cursors.Arrow,
                     ValueForTrue = Cursors.Hand
                  }
               });
            // background
            var bg_brush = new SolidColorBrush();
            BindingOperations.SetBinding(bg_brush, SolidColorBrush.ColorProperty, new Binding(nameof(background_color)) { Source = this });
            hl.SetCurrentValue(HyperlinkCell.BackgroundProperty, bg_brush);
            // style
            if (ElementStyle != null)
               hl.Style = ElementStyle;
            view_generated?.Invoke(hl);
            return hl;
         }
      }
   }
}
