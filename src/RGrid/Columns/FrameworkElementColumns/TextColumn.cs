using RGrid.Controls;
using RGrid.Filters;
using System.Windows;
using System.Windows.Data;

namespace RGrid {
   public partial class DataGrid {
      public class TextColumn<TColKey> : FrameworkElementColumnBase<TColKey>, IFilterableColumn {
         readonly BindingBase _binding;

         public TextColumn(TColKey key, BindingBase binding, IDataGridColumnFilter filter, string filter_template) : base(key) {
            this.filter = filter;
            this.filter_template = filter_template;
            _binding = binding;
         }

         public IDataGridColumnFilter filter { get; }
         public string filter_template { get; }
         public BindingBase background_binding { get; set; }
         public BindingBase font_weight_binding { get; set; }
         public BindingBase foreground_binding { get; set; }
         public BindingBase tooltip_binding { get; set; }
         public VerticalAlignment vertical_alignment { get; set; } = VerticalAlignment.Center;

         public override FrameworkElement view_factory() {
            var v = new TextCell { FontFamily = font_family, FontSize = font_size, Foreground = font_brush, HorizontalAlignment = horizontal_alignment, VerticalAlignment = vertical_alignment };
            v.SetBinding(TextCell.TextProperty, _binding);
            try_set_binding(TextCell.BackgroundProperty, background_binding);
            try_set_binding(TextCell.ForegroundProperty, foreground_binding);
            try_set_binding(TextCell.FontWeightProperty, font_weight_binding);
            try_set_binding(TextCell.ToolTipProperty, tooltip_binding);
            return v;
            //
            void try_set_binding(DependencyProperty p, BindingBase b) {
               if (b != null)
                  v.SetBinding(p, b);
            }
         }
      }
   }
}