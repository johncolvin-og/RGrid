using RGrid.WPF;
using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace RGrid {
   public interface ISelectionHighlightListener {
      void on_selection_fill(Brush selection_fill);
   }

   public interface IDataGridColumn {
      string ID { get; }
   }

   interface IFilterableColumn : IDataGridColumn {
      Filters.IDataGridColumnFilter filter { get; }
      string filter_template { get; }
   }

   interface IFilterableColumn<T> : IFilterableColumn {
      new Filters.IDataGridColumnFilter<T> filter { get; }
   }

   // To include extra data with your column type you are encouraged to derive from RGridColumn
   public abstract class ColumnBase : GridViewColumn, IDataGridColumn, INotifyPropertyChanged {
      //readonly ExternalProperty<FontWeight?> _font_weigh
      FontWeight _fallback_font_weight = FontWeights.Normal;
      Color _fallback_font_color = Colors.Black;
      double _fallback_font_size = 12;
      System.Windows.Media.FontFamily _fallback_font_family = new System.Windows.Media.FontFamily("Segoe UI");
      FontWeight? _font_weight_override;
      Color? _font_color_override;
      object _header_copy;
      bool _is_visible;

      public static readonly DependencyProperty SortDirectionProperty =
         WPFHelper.create_dp<ListSortDirection?, ColumnBase>(
            nameof(SortDirection), (o, v) => o.OnSortDirectionChanged(v));

      public ListSortDirection? SortDirection {
         get => GetValue(SortDirectionProperty) as ListSortDirection?;
         set => SetValue(SortDirectionProperty, value);
      }

      public event Action<ColumnBase, ListSortDirection?> sort_direction_changed;
      public event Action font_params_changed;
      public event Action<bool> is_visible_changed;


      public string ID { get; set; }
      public string SortMemberPath { get; set; }
      public double original_width { get; set; }
      public bool is_collapsed { get; set; }
      public bool is_visible {
         get => _is_visible;
         set {
            if (_is_visible != value) {
               _is_visible = value;
               is_visible_changed?.Invoke(value);
               raise_prop_changed();
            }
         }
      }
      public FontWeight? font_weight_override { get => _font_weight_override; set => _set_font_param(ref _font_weight_override, value); }
      public Color? font_color_override { get => _font_color_override; set => _set_font_param(ref _font_color_override, value); }
      public HorizontalAlignment horizontal_alignment { get; set; }
      public double horizontal_offset { get; set; }
      public FontFamily font_family => _fallback_font_family;
      public double font_size => _fallback_font_size; //TODO: font_size override
      public FontWeight font_weight => font_weight_override ?? _fallback_font_weight;
      public Color font_color => font_color_override ?? _fallback_font_color;
      public Brush font_brush => new SolidColorBrush(font_color);
      // TODO: find a better way to store the header-property in a thread-safe manner - a value for GridSettings.display_name, more specifically. 
      // Since save_state is called from a different thread in IShared/SlowWindows, it is not safe to access the DependencyProperty store from that method.
      // By updating a local _header_copy field in the Header setter, the DependencyProperty lookup may be bypassed when appropriate (see create method below).
      public new object Header {
         get => base.Header;
         set => base.Header = (_header_copy = value);
      }

      void _set_font_param<T>(ref T field, T value) {
         field = value;
         font_params_changed?.Invoke();
      }

      // 'fallback' font-params (aka 'grid-level' font-params)
      public void set_fallback_font_params(FontFamily font_family, double font_size, Color font_color, FontWeight font_weight) {
         _fallback_font_family = font_family;
         _fallback_font_size = font_size;
         _fallback_font_color = font_color;
         _fallback_font_weight = font_weight;
         font_params_changed?.Invoke();
      }

      protected virtual void OnSortDirectionChanged(ListSortDirection? new_value) =>
         sort_direction_changed?.Invoke(this, new_value);

      protected void raise_prop_changed([CallerMemberName]string prop = "") =>
         OnPropertyChanged(new PropertyChangedEventArgs(prop));
   }

   public abstract class ColumnBase<TColKey> : ColumnBase, IKeyedColumn<TColKey> {
      protected ColumnBase(TColKey key) => this.key = key;

      public TColKey key { get; }
   }
}