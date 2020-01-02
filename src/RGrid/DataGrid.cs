using Collections.Sync;
using Disposable.Extensions;
using Disposable.Extensions.Utilities;
using EqualityComparer.Extensions;
using Monitor.Render.Utilities;
using RGrid.Controls;
using RGrid.Filters;
using RGrid.Utility;
using RGrid.WPF;
using RGrid.WPF.Converters;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Reactive.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;

namespace RGrid {
   public interface IHandleGridSorting {
      void on_sorting(object sender, DataGridSortingEventArgs e);
   }

   public interface IRequestRowSelection {
      event Action<(IEnumerable, bool)> request_selection;
   }

   public class DataGridColumnCollection : ObservableCollection<ColumnBase> { }
   public class DataGridSortingEventArgs : EventArgs {
      public DataGridSortingEventArgs(ColumnBase column) => Column = column;
      public ColumnBase Column { get; }
      public bool Handled { get; set; }
   }
   public delegate void DataGridSortingEventHandler(object sender, DataGridSortingEventArgs e);


   public class CachedColumnParam {
      public string SourcePropertyName { get; set; }
      public DependencyProperty TargetProperty { get; set; }
   }

   public class GroupItemSelectionPivotEventArgs : RoutedEventArgs {
      public GroupItemSelectionPivotEventArgs(RoutedEvent routed_event, object source, CollectionViewGroup group_item)
         : base(routed_event, source) => this.group_item = group_item;

      public CollectionViewGroup group_item { get; }
   }


   public partial class DataGrid : Control {
      public const double
         DefaultRowHeight = 17,
         DefaultColumnHeaderHeight = 30;
      public static readonly Thickness
         DefaultCellPadding = new Thickness(4, 2, 4, 2),
         AnimatableCellPadding = new Thickness(8, 2, 4, 2);
      public static readonly Visibility DefaultRowsHorizontalScrollBarVisibility = Visibility.Visible;
      bool _support_group_items;

      #region RoutedCommands
      public static readonly RoutedCommand SortCommand = new RoutedCommand(), DefaultRowCommand = new RoutedCommand();
      #endregion
      // TODO: SelectionFill/Stroke should not be hardcoded, for now this serves as the central location for DragRectAdorner,
      // and FrameworkElementColumns that paint the selection brush themselves
      // (e.g MarketView bid/ask columns, bc their background is not transparent, it covers the ListBoxItem selection-highlight).
      public static readonly Color DefaultSelectionHighlight = Colors.MediumSlateBlue.with(0x63);

      readonly NotifyCollectionChangedEventHandler _sort_descriptions_collection_changed_cb;
      DataContextHooks _data_context_hooks;

      public static readonly DependencyProperty SelectionHighlightProperty =
         WPFHelper.create_dp<Color, DataGrid>(nameof(SelectionHighlight), DefaultSelectionHighlight);

      public Color SelectionHighlight {
         get => (Color)GetValue(SelectionHighlightProperty);
         set => SetValue(SelectionHighlightProperty, value);
      }

      static DataGrid() {
         DefaultStyleKeyProperty.OverrideMetadata(typeof(DataGrid), new FrameworkPropertyMetadata(typeof(DataGrid)));
         UseLayoutRoundingProperty.OverrideMetadata(typeof(DataGrid), new FrameworkPropertyMetadata(OnUseLayoutRoundingChanged));

         void OnUseLayoutRoundingChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) =>
            d.CoerceValue(RowHeightProperty);
      }

      public DataGrid() {
         CommandBindings.Add(new CommandBinding(ApplicationCommands.Copy, OnCopy, OnCopyCanExecute));
         CommandBindings.Add(new CommandBinding(SortCommand, (s, e) => {
            if (e.Parameter is GridViewColumnHeader col_header && col_header.Column is ColumnBase cb) {
               var sorting_event = Sorting;
               if (sorting_event != null) {
                  var sorting_args = new DataGridSortingEventArgs(cb);
                  sorting_event(this, sorting_args);
               }
            }
         }));
         _data_context_hooks = new DataContextHooks(this, DataContext);
         DataContextChanged += OnDataContextChanged;
      }

      public event DataGridSortingEventHandler Sorting;
      public event Action<bool> SupportGroupItemsChanged;

      public static IValueConverter SelectionHighlightPenConverter =>
         ValueConverter.create<Color, Pen>(c => FrozenPenCache.get_pen(c.with(255)));

      public static IValueConverter SelectionHighlightBrushConverter =>
         ValueConverter.create<Color, Brush>(FrozenBrushCache.get_brush);

      public static IValueConverter SelectionHighlightOpaqueBrushConverter =>
         ValueConverter.create<Color, Brush>(c => FrozenBrushCache.get_brush(c.with(255)));

      public static IValueConverter SelectionHighlightOpacityConverter =>
         ValueConverter.create<bool, double>(is_active => is_active ? 1.0 : 0.5);

      public static IValueConverter RowHoverHighlightBrushConverter =>
         ValueConverter.create<Color, Brush>(c => FrozenBrushCache.get_brush(c, 0.35));

      public static IValueConverter CellHoverHighlightPenConverter =>
         ValueConverter.create<Color, Pen>(c => FrozenPenCache.get_pen(c.with(255)));

      public static IValueConverter CellHoverHighlightBrushConverter =>
         ValueConverter.create<Color, Brush>(c => FrozenBrushCache.get_brush(c, 0.5));

      public bool SupportGroupItems {
         get => _support_group_items;
         set {
            if (_support_group_items != value) {
               _support_group_items = value;
               SupportGroupItemsChanged?.Invoke(value);
            }
         }
      }

      public bool SyncColumnSortDirectionWithSortDescriptions { get; set; } = true;

      public ItemContainerGenerator RowContainerGenerator =>
         _row_presenter.ItemContainerGenerator;

      public ObservableCollection<GroupStyle> GroupStyle { get; } = new ObservableCollection<GroupStyle>();

      private void OnCopy(object sender, ExecutedRoutedEventArgs e) {
         // TODO: implement excel-style copy rows/cells
      }

      private void OnCopyCanExecute(object sender, CanExecuteRoutedEventArgs e) {
         e.CanExecute = true;
      }

      // Private Properties
      #region Private fields

      GridViewHeaderRowPresenter _header_frozen;
      GridViewHeaderRowPresenter _header_mobile;
      ScrollBar _scroll_horizontal;
      ListBox _row_presenter;
      ScrollViewer _header_scroll_viewer;
      ScrollBar _header_horizontal_scroll_bar;

      GridViewColumnCollection _frozen_columns = new GridViewColumnCollection();
      GridViewColumnCollection _scroll_columns = new GridViewColumnCollection();
      int _frozen_column_count;
      int _get_visible_frozen_column_count() {
         int vis_frozen_col_count = 0;
         if (Columns is ObservableCollection<ColumnBase> all_columns) {
            for (int i = 0; i < _frozen_column_count; i++) {
               if (i == visible_columns.Count)
                  break;
               else if (visible_columns[i].Equals(all_columns[i]))
                  ++vis_frozen_col_count;
            }
         }
         return vis_frozen_col_count;
      }

      #endregion

      #region Private Methods

      void OnDataContextChanged(object sender, DependencyPropertyChangedEventArgs e) {
         _data_context_hooks?.Dispose();
         _data_context_hooks = new DataContextHooks(this, e.NewValue);
      }

      #region Column Handling
      public void invalidate_column_layout(object sender = null, EventArgs args = null) {
         var geometry = GetColumnGeometry().ToList();
         var clc = ColumnLayoutChanged;
         if (clc != null) {
            clc.Invoke(geometry);
         }

#if DEBUG
         // Due to the intricacies of WPF layout this would be very easy to regress so I'm leaving this code in here as a safeguard
         var left = 0.0;
         foreach (var g in geometry) {
            Debug.Assert(left <= g.left);
            if (left > g.left) {
               string geometrystr = string.Join(" ", geometry.Select(r => r.ToString()));
               Console.WriteLine(geometrystr);
               break;
            }
            left = g.left;
         }
#endif
      }

      internal readonly ObservableCollection<ColumnBase> visible_columns = new ObservableCollection<ColumnBase>();
      IDisposable _columns_hooks;

      private static void columns_changed(DependencyObject d, DependencyPropertyChangedEventArgs args) {
         if (d is DataGrid grid) {
            grid._reset_columns(args.NewValue as ObservableCollection<ColumnBase>);
         }
      }

      void _reset_columns() =>
         _reset_columns(Columns);

      // Called when the columns COLLECTION has been replaced entirely
      void _reset_columns(ObservableCollection<ColumnBase> newval) {
         DisposableUtils.Dispose(ref _columns_hooks);
         if (newval != null) {
            // When column.is_visible changes, visible_columns will be refreshed asynchronously,
            // via Dispatcher.InvokeAsync, to drown out noisy batch changes. 
            // The vis_cols_sync_queued flag indicates that such a refresh has already been queued.
            bool vis_cols_sync_queued = false;
            // vis_cols_disposed, when true, indicates that the Columns dp has changed.
            // This flag is checked in the Dispatcher.InvokeAsync callback before resetting visible_columns, to avoid overwriting the latest value.
            bool vis_cols_disposed = false;
            var vis_cols_hook = new System.Reactive.Disposables.MultipleAssignmentDisposable();
            reset_visible_columns();
            _columns_hooks = DisposableFactory.Create(
               DisposableFactory.Create(() => vis_cols_disposed = true),
               ObservableAutoWrapper.ConnectItemHooks(newval, c => {
                  c.is_visible_changed += column_is_visible_changed;
                  return DisposableFactory.Create(() => c.is_visible_changed -= column_is_visible_changed);
               }),
               vis_cols_hook,
               visible_columns.Subscribe(on_columns_changed));
            if (GetCachedColumnParamsContainer(this) is FrameworkElement params_container && CachedColumnParams is IEnumerable<string> ccp) {
               var ccp_array = ccp.ToArray();
               _columns_hooks = DisposableFactory.Create(
                  _columns_hooks,
                  ObservableAutoWrapper.ConnectItemHooks(newval, c =>
                     ObjectSnapshot.property_table((INotifyPropertyChanged)c, ccp_array)
                     .Subscribe((TableUpdate<(string name, object current, object original), string> table) => {
                        foreach (var (name, current, original) in table.added.Concat(table.changed))
                           params_container.Resources[ColumnResourceHelper.col_prop_resource_id(c.ID, name)] = current;
                     })
                  ));
            }
            // local methods
            void reset_visible_columns() =>
               vis_cols_hook.Disposable = ObservableFilterCollection.connect_never_duplicate(newval, visible_columns, c => c.is_visible, nameof(ColumnBase.is_visible));
               //vis_cols_hook.replace(ObservableFilterCollection.connect_never_duplicate(newval, _visible_columns, c => c.is_visible, nameof(ColumnBase.is_visible)));

            void column_is_visible_changed(bool is_visible) {
               if (!vis_cols_sync_queued) {
                  vis_cols_sync_queued = true;
                  Dispatcher.InvokeAsync(() => {
                     if (!vis_cols_disposed) {
                        reset_visible_columns();
                        vis_cols_sync_queued = false;
                     }
                  });
               }
            }
         }
         on_columns_changed(visible_columns, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
      }

      // Called when items are added or removed from the columns collection
      void on_columns_changed(object sender, NotifyCollectionChangedEventArgs e) {
         if (sender is ObservableCollection<ColumnBase> columns) {
            Debug.Assert(sender == visible_columns, "DataGrid.on_columns_changed callback expects the sender to be the grid's visible_columns (or null).");
            // frozen_column_count is with respect to all columns, visible or not,
            // need to account for the fact that a "frozen" column might not be visible
            var all_columns = Columns;

            int vis_frozen_col_count = _get_visible_frozen_column_count();
            _frozen_columns.removeRange(columns.Skip(vis_frozen_col_count));
            _scroll_columns.sync_with(columns.Skip(vis_frozen_col_count));
            _frozen_columns.sync_with(columns.Take(vis_frozen_col_count));
            invalidate_column_layout();
            switch (e.Action) {
               case NotifyCollectionChangedAction.Add:
                  foreach (ColumnBase col in e.NewItems) {
                     _try_set_column_filter_props(col);
                     col.font_params_changed += _on_col_font_params_changed;
                  }
                  break;
               case NotifyCollectionChangedAction.Remove:
                  foreach (ColumnBase col in e.OldItems) {
                     col.font_params_changed -= _on_col_font_params_changed;
                  }
                  break;
               case NotifyCollectionChangedAction.Reset:
                  foreach (var col in columns) {
                     _try_set_column_filter_props(col);
                     col.font_params_changed += _on_col_font_params_changed;
                  }
                  break;
            }
            var binding = new MultiBinding() { Converter = SumConverter.instance };
            foreach (var element in columns) {
               binding.Bindings.Add(new Binding() {
                  Path = new PropertyPath("ActualWidth"),
                  Source = element
               });
            }
            SetBinding(HeaderTotalSizeHackProperty, binding);
         } else {
            _frozen_columns.Clear();
            _scroll_columns.Clear();
         }
         CoerceValue(RowHeightProperty);
      }

      void _on_col_font_params_changed() => CoerceValue(RowHeightProperty);

      void _try_set_column_filter_props(ColumnBase column) {
         if (column.HeaderTemplate == null && !(column.Header is Visual))
            column.HeaderTemplate = TryFindResource("DefaultColumnHeaderContentTemplate") as DataTemplate;
         if (column is IFilterableColumn fc) {
            DataGridFilterProperties.SetFilter(column, fc.filter);
            DataGridFilterProperties.SetFilterTemplate(column, FindResource(fc.filter_template) as DataTemplate);
            BindingOperations.SetBinding(column, DataGridFilterProperties.FilterActiveProperty, new Binding(nameof(IDataGridColumnFilter.active)) { Source = fc.filter });
            BindingOperations.SetBinding(column, DataGridFilterProperties.FilterOpenProperty, new Binding(nameof(IDataGridColumnFilter.open)) { Source = fc.filter });
         }
      }

      public static readonly DependencyProperty HeaderTotalSizeHackProperty = DependencyProperty.Register("HeaderTotalSizeHack", typeof(double), typeof(DataGrid), new FrameworkPropertyMetadata(0.0, dp_invalidate_column_layout));

      public double HeaderTotalSizeHack {
         get => (double)GetValue(HeaderTotalSizeHackProperty);
         set => SetValue(HeaderTotalSizeHackProperty, value);
      }

      private static void dp_invalidate_column_layout(DependencyObject d, DependencyPropertyChangedEventArgs unused) {
         var grid = d as DataGrid;
         if (grid != null) {
            grid.invalidate_column_layout();
         }
      }
      #endregion

      #region Scrolling
      ScrollBar get_scrollbar(FrameworkElement scrollviewer, Orientation orientation) {
         if (scrollviewer == null) return null;
         return scrollviewer.descendants_of_type<ScrollBar>().Where(sb => sb.Orientation == orientation).FirstOrDefault();
      }

      void on_horizontal_scroll(object sender, RoutedPropertyChangedEventArgs<double> e) {
         if (_header_scroll_viewer != null) {
            if (_header_horizontal_scroll_bar != null) {
               if (Math.Abs(e.NewValue - _header_horizontal_scroll_bar.Value) > MathUtils.epsilon * 10) {
                  _header_scroll_viewer.ScrollToHorizontalOffset(e.NewValue);
                  invalidate_column_layout();
               }
            }
         }
      }

      #endregion

      #endregion

      // Public Properties
      #region Public Properties

      #region Column
      public static readonly DependencyProperty ColumnProperty =
         DependencyProperty.RegisterAttached("Column", typeof(ColumnBase), typeof(DataGrid));

      public static ColumnBase GetColumn(DependencyObject d) => d.GetValue(ColumnProperty) as ColumnBase;
      public static void SetColumn(DependencyObject d, ColumnBase value) => d.SetValue(ColumnProperty, value);
      #endregion

      #region Grid
      public static readonly DependencyProperty GridProperty =
         DependencyProperty.RegisterAttached("Grid", typeof(DataGrid), typeof(DataGrid));

      public static DataGrid GetGrid(DependencyObject d) => d.GetValue(GridProperty) as DataGrid;
      public static void SetGrid(DependencyObject d, DataGrid value) => d.SetValue(GridProperty, value);
      #endregion

      #region ColumnKey
      public static readonly DependencyProperty ColumnKeyProperty =
      DependencyProperty.RegisterAttached("ColumnKey",
         typeof(object),
         typeof(DataGrid),
         new PropertyMetadata((d, e) => {
            if (!(d is UIElement child))
               return;
            if (VisualTreeHelper.GetParent(child) is Grid panel) {
               if (GetSyncWithColumnGeometrySource(d) is DataGrid diy_grid || panel.try_find_ancestor_of_type(out diy_grid))
                  ColumnGeometrySyncHelper.reset_panel_child(child, diy_grid, e.NewValue);
            }
            if (d is FrameworkElement child_el) {
               var curr_params = GetObservedCachedColumnParams(d);
               if (curr_params != null) {
                  // clear observed params with old key, then reset with new key
                  ColumnResourceHelper.reset_cell_resource_references(child_el, e.OldValue, curr_params, null);
                  ColumnResourceHelper.reset_cell_resource_references(child_el, e.NewValue, null, curr_params);
               }
            }
         }));

      public static object GetColumnKey(DependencyObject d) => d.GetValue(ColumnKeyProperty);
      public static void SetColumnKey(DependencyObject d, object value) => d.SetValue(ColumnKeyProperty, value);
      #endregion

      #region SyncWithColumnGeometrySource
      public static readonly DependencyProperty SyncWithColumnGeometrySourceProperty =
         DependencyProperty.RegisterAttached(
            "SyncWithColumnGeometrySource",
            typeof(DataGrid),
            typeof(DataGrid),
            new PropertyMetadata(ColumnGeometrySyncHelper.reset_sync));

      public static DataGrid GetSyncWithColumnGeometrySource(DependencyObject d) => d.GetValue(SyncWithColumnGeometrySourceProperty) as DataGrid;
      public static void SetSyncWithColumnGeometrySource(DependencyObject d, DataGrid value) => d.SetValue(SyncWithColumnGeometrySourceProperty, value);
      #endregion

      #region SyncWithColumnGeometry
      public static readonly DependencyProperty SyncWithColumnGeometryProperty =
         DependencyProperty.RegisterAttached(
            "SyncWithColumnGeometry",
            typeof(bool),
            typeof(DataGrid),
            new PropertyMetadata(false, ColumnGeometrySyncHelper.reset_sync));

      public static bool GetSyncWithColumnGeometry(DependencyObject d) => (bool)d.GetValue(SyncWithColumnGeometryProperty);
      public static void SetSyncWithColumnGeometry(DependencyObject d, bool value) => d.SetValue(SyncWithColumnGeometryProperty, value);

      static readonly DependencyPropertyKey SyncWithColumnGeometryHookPropertyKey =
         DependencyProperty.RegisterAttachedReadOnly(
            "SyncWithColumnGeometryHook",
            typeof(IDisposable),
            typeof(DataGrid),
            new PropertyMetadata(FrameworkElementLifeDependencyHelper.DisposeOld));

      internal static IDisposable GetSyncWithColumnGeometryHook(DependencyObject d) =>
         d.GetValue(SyncWithColumnGeometryHookPropertyKey.DependencyProperty) as IDisposable;

      static void SetSyncWithColumnGeometryHook(DependencyObject d, IDisposable value) =>
         d.SetValue(SyncWithColumnGeometryHookPropertyKey, value);
      #endregion

      #region CachedColumnParamsContainer
      // This is the element who's ResourceDictionary will be used to store the observed column params.
      // By allowing callers to specify this element (as opposed to simply putting them in the DIYGrid's resources),
      // elements that are not visual children of the DIYGrid can bind to these params as well.
      // Once assigned, this value should not change during the lifetime of the DIYGrid.
      // This could create conflicting references among the binding targets,
      // as resources added to the old CachedColumnParamsContainer would not be removed.
      public static readonly DependencyProperty CachedColumnParamsContainerProperty =
         DependencyProperty.RegisterAttached(
            "CachedColumnParamsContainer",
            typeof(FrameworkElement),
            typeof(DataGrid),
            new PropertyMetadata((d, e) => (d as DataGrid)?._reset_columns()));

      public static FrameworkElement GetCachedColumnParamsContainer(DependencyObject d) => d.GetValue(CachedColumnParamsContainerProperty) as FrameworkElement;
      public static void SetCachedColumnParamsContainer(DependencyObject d, ResourceDictionary value) => d.SetValue(CachedColumnParamsContainerProperty, value);
      #endregion

      #region CachedColumnParams
      public static readonly DependencyProperty CachedColumnParamsProperty =
         WPFHelper.create_dp<IEnumerable<string>, DataGrid>(
            nameof(CachedColumnParams),
            (o, v) => {
               o?._reset_columns();
            });

      public IEnumerable<string> CachedColumnParams {
         get => GetValue(CachedColumnParamsProperty) as IEnumerable<string>;
         set => SetValue(CachedColumnParamsProperty, value);
      }
      #endregion

      #region ObservedCachedColumnParams
      public static readonly DependencyProperty ObservedCachedColumnParamsProperty =
         DependencyProperty.RegisterAttached(
            "ObservedCachedColumnParams",
            typeof(IEnumerable<CachedColumnParam>),
            typeof(DataGrid),
           new PropertyMetadata(ColumnResourceHelper.ObservedCachedParamsChanged));

      public static IEnumerable<CachedColumnParam> GetObservedCachedColumnParams(DependencyObject d) => d.GetValue(ObservedCachedColumnParamsProperty) as IEnumerable<CachedColumnParam>;
      public static void SetObservedCachedColumnParams(DependencyObject d, IEnumerable<CachedColumnParam> value) => d.SetValue(ObservedCachedColumnParamsProperty, value);
      #endregion

      public static readonly DependencyProperty BackgroundOverlayProperty =
         DependencyProperty.RegisterAttached(
            "BackgroundOverlay",
            typeof(Brush),
            typeof(DataGrid),
            new FrameworkPropertyMetadata(
               null,
               FrameworkPropertyMetadataOptions.AffectsRender));

      public static Brush GetBackgroundOverlay(DependencyObject d) => d.GetValue(BackgroundOverlayProperty) as Brush;
      public static void SetBackgroundOverlay(DependencyObject d, Brush value) => d.SetValue(BackgroundOverlayProperty, value);

      #region RowStyle
      public static readonly DependencyProperty RowStyleProperty =
         WPFHelper.create_dp<Style, DataGrid>(
            nameof(RowStyle),
            (o, s) => {
               o._row_style = s;
               o._try_set_row_style();
            });

      Style _row_style;
      void _try_set_row_style() {
         if (_row_presenter != null && _row_style != null)
            _row_presenter.ItemContainerStyle = _row_style;
      }

      public Style RowStyle { get => GetValue(RowStyleProperty) as Style; set => SetValue(RowStyleProperty, value); }
      #endregion

      public static readonly DependencyProperty CurrentRowProperty = DependencyProperty.Register(
         nameof(CurrentRow), typeof(object), typeof(DataGrid), new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault));

      public object CurrentRow { get => GetValue(CurrentRowProperty); set => SetValue(CurrentRowProperty, value); }

      // Use DIYGridColumnCollection if you want to assign in XAML
      public ObservableCollection<ColumnBase> Columns {
         get { return (ObservableCollection<ColumnBase>)GetValue(ColumnsProperty); }
         set { SetValue(ColumnsProperty, value); }
      }

      // Use DIYGridColumnCollection if you want to assign in XAML
      public static readonly DependencyProperty ColumnsProperty =
          DependencyProperty.Register("Columns", typeof(ObservableCollection<ColumnBase>), typeof(DataGrid),
             new FrameworkPropertyMetadata(null, columns_changed));

      public Style ColumnHeaderStyle {
         get { return (Style)GetValue(ColumnHeaderStyleProperty); }
         set { SetValue(ColumnHeaderStyleProperty, value); }
      }

      public static readonly DependencyProperty ColumnHeaderStyleProperty =
          DependencyProperty.Register("ColumnHeaderStyle", typeof(Style), typeof(DataGrid), new PropertyMetadata(null));

      public int FrozenColumnCount {
         get { return (int)GetValue(FrozenColumnCountProperty); }
         set { SetValue(FrozenColumnCountProperty, value); }
      }

      public static readonly DependencyProperty FrozenColumnCountProperty =
          DependencyProperty.Register("FrozenColumnCount", typeof(int), typeof(DataGrid), new FrameworkPropertyMetadata(0, on_frozen_column_count_changed));
      private static void on_frozen_column_count_changed(DependencyObject obj, DependencyPropertyChangedEventArgs args) {
         DataGrid grid = (DataGrid)obj;
         grid._frozen_column_count = (int)args.NewValue;
         grid.on_columns_changed(obj, null);
      }

      public double RowHeight {
         get { return (double)GetValue(RowHeightProperty); }
         set { SetValue(RowHeightProperty, value); }
      }

      public static readonly DependencyProperty RowHeightProperty = WPFHelper.create_dp<double, DataGrid>(
         nameof(RowHeight),
         (o, v) => o.RowHeightChanged?.Invoke(v),
         (o, v) => {
            if (DoubleHelper.IsNaNOrInfinity(v))
               v = 0;
            if (!o.visible_columns.is_null_or_empty()) {
               double max_font_size = o.visible_columns.Max(c => c.font_size);
               double min_row_height = o.RowPadding.Top + max_font_size + o.RowPadding.Bottom;
               if (v < min_row_height)
                  return min_row_height;
            }
            return o.UseLayoutRounding ? v.RoundLayoutValue() : v;
         }, DefaultRowHeight);

      public event Action<double> RowHeightChanged;

      public Thickness RowPadding {
         get { return (Thickness)GetValue(RowPaddingProperty); }
         set { SetValue(RowPaddingProperty, value); }
      }

      public static readonly DependencyProperty RowPaddingProperty = DependencyProperty.Register(
         nameof(RowPadding), typeof(Thickness), typeof(DataGrid), new PropertyMetadata(DefaultCellPadding));

      public IEnumerable Rows {
         get { return (IEnumerable)GetValue(RowsProperty); }
         set { SetValue(RowsProperty, value); }
      }

      public static readonly DependencyProperty RowsProperty =
         WPFHelper.create_dp<IEnumerable, DataGrid>(
            nameof(Rows),
            (s, oldval, newval) =>
            WPFHelper.manage_collection_change(
               (oldval as ICollectionView)?.SortDescriptions,
               (newval as ICollectionView)?.SortDescriptions,
               s._sort_descriptions_collection_changed_cb),
            Enumerable.Empty<object>());

      public DataTemplate RowTemplate {
         get { return (DataTemplate)GetValue(RowTemplateProperty); }
         set { SetValue(RowTemplateProperty, value); }
      }

      public static readonly DependencyProperty RowTemplateProperty =
          DependencyProperty.Register("RowTemplate", typeof(DataTemplate), typeof(DataGrid), new PropertyMetadata(null));

      public event Action<List<ColumnGeometry>> ColumnLayoutChanged;

      public Typeface GetFontFace() {
         return new Typeface(
            (FontFamily)GetValue(FontFamilyProperty),
            (FontStyle)GetValue(FontStyleProperty),
            (FontWeight)GetValue(FontWeightProperty),
            (FontStretch)GetValue(FontStretchProperty),
            (FontFamily)GetValue(FontFamilyProperty));
      }

      public SelectionMode SelectionMode {
         get { return (SelectionMode)GetValue(SelectionModeProperty); }
         set { SetValue(SelectionModeProperty, value); }
      }

      public static readonly DependencyProperty SelectionModeProperty = ListBox.SelectionModeProperty.AddOwner(typeof(DataGrid));

      public Visibility RowsHorizontalScrollBarVisibility {
         get { return (Visibility)GetValue(RowsHorizontalScrollBarVisibilityProperty); }
         set { SetValue(RowsHorizontalScrollBarVisibilityProperty, value); }
      }

      public static readonly DependencyProperty RowsHorizontalScrollBarVisibilityProperty = DependencyProperty.Register(
         nameof(RowsHorizontalScrollBarVisibility), typeof(Visibility), typeof(DataGrid), new PropertyMetadata(DefaultRowsHorizontalScrollBarVisibility));

      #endregion

      // Internal Methods
      #region Internal Properties/Methods
      /// Compute the geometry of all visible (or partially visible columns) columns
      /// This effectively replaces the entire Measure/Arrange pass of the grid rows (and enables column virtualization)
      /// Mouse events will be hit tested against this as well
      internal IEnumerable<ColumnGeometry> GetColumnGeometry(bool include_obscured_cols = false) {
         if (_header_scroll_viewer != null && ActualWidth > 0) {
            double frozen_offset = 0;
            int vis_frozen_col_count = _get_visible_frozen_column_count();
            foreach (var col in visible_columns.Take(vis_frozen_col_count)) {
               double right = frozen_offset + col.ActualWidth;
               right = right.RoundLayoutValue();
               yield return new ColumnGeometry(col, frozen_offset, frozen_offset, right);
               frozen_offset += col.ActualWidth;
            }
            if (_frozen_column_count > 0) {
               // frozen_offset will be off by 2 pixels because of the GridViewHeaderRowPresenter PaddingHeader (see c_PaddingHeaderMinWidth in referencesource)
               // We could use the _header_frozen.ActualWidth but due to the async nature of layout it get updated slightly too late
               frozen_offset += 2;
            }
            // Adjust by scroll amount
            double offset = frozen_offset - _scroll_horizontal.Value;
            offset = offset.RoundLayoutValue();
            int count = 0;
            foreach (var col in visible_columns.Skip(vis_frozen_col_count)) {
               double left = Math.Max(offset, frozen_offset);
               double right = offset + col.ActualWidth;
               right = right.RoundLayoutValue();
               // If right < frozen_offset column is completely obscured
               if (include_obscured_cols || (right > frozen_offset && left <= ActualWidth)) {
                  // fully visible
                  yield return new ColumnGeometry(col, left, offset, right);
               }
               offset = right;
               ++count;
            }
         }
      }

      internal void scroll_horizontal(double delta) {
         _scroll_horizontal.Value += delta;
      }

      #endregion

      IDisposable _group_style_row_presenter_link;
      // Overrides
      #region Overrides
      public override void OnApplyTemplate() {
         base.OnApplyTemplate();
         _header_frozen = GetTemplateChild("_header_frozen") as GridViewHeaderRowPresenter;
         _header_frozen.Columns = _frozen_columns;
         _header_mobile = GetTemplateChild("_header_scroll") as GridViewHeaderRowPresenter;
         _header_mobile.Columns = _scroll_columns;

         _scroll_horizontal = (ScrollBar)GetTemplateChild("_scroll_horiz");
         _row_presenter = (ListBox)GetTemplateChild("_row_presenter");
         Debug.Assert(!_row_presenter.IsLoaded);
         // (re)wrap group style
         _group_style_row_presenter_link?.Dispose();
         _group_style_row_presenter_link = ObservableAutoWrapper.SynchronizeTwoWay(GroupStyle, _row_presenter.GroupStyle);
         _try_set_row_style();
         _header_scroll_viewer = (ScrollViewer)GetTemplateChild("_header_scroll_viewer");
         Debug.Assert(!_header_scroll_viewer.IsLoaded);
         _header_scroll_viewer.Loaded += (o, e) => {
            _header_horizontal_scroll_bar = get_scrollbar(_header_scroll_viewer, Orientation.Horizontal);
            Debug.Assert(_header_horizontal_scroll_bar != null);
            Action<DependencyProperty> bind =
               prop => _scroll_horizontal.SetBinding(prop, new Binding(prop.Name) { Source = _header_horizontal_scroll_bar });
            bind(ScrollBar.SmallChangeProperty);
            bind(ScrollBar.LargeChangeProperty);
            bind(ScrollBar.MinimumProperty);
            bind(ScrollBar.MaximumProperty);
            bind(ScrollBar.ViewportSizeProperty);
         };

         _scroll_horizontal.ValueChanged += on_horizontal_scroll;
         _data_context_hooks?.Dispose();
         _data_context_hooks = new DataContextHooks(this, DataContext);
      }

      protected override Size ArrangeOverride(Size arrangeBounds) {
         invalidate_column_layout();
         return base.ArrangeOverride(arrangeBounds);
      }

      protected override Size MeasureOverride(Size constraint) {
         invalidate_column_layout();
         return base.MeasureOverride(constraint);
      }

      protected override void OnRenderSizeChanged(SizeChangedInfo sizeInfo) {
         invalidate_column_layout();
         base.OnRenderSizeChanged(sizeInfo);
      }

      protected override void OnMouseDoubleClick(MouseButtonEventArgs e) {
         base.OnMouseDoubleClick(e);
         if (e.ChangedButton == MouseButton.Left)
            InputBindings.OfType<MouseBinding>().ApplyToFirst(
               mb => mb.MouseAction == MouseAction.LeftDoubleClick,
               mb => {
                  if (mb.Command.CanExecute(mb.CommandParameter))
                     mb.Command.Execute(mb.CommandParameter);
               });
      }
      #endregion

      static class ColumnGeometrySyncHelper {
         public static void reset_panel_child(UIElement child, DataGrid diy_grid) =>
            reset_panel_child(child, diy_grid, GetColumnKey(child));

         public static void reset_panel_child(UIElement child, DataGrid diy_grid, object col_key) {
            if (col_key == null)
               return;
            var key_str = col_key.ToString();
            if (diy_grid.visible_columns.TryGetFirst(c => string.Equals(c.ID, key_str, StringComparison.CurrentCultureIgnoreCase), out var target_col, out int target_pos)) {
               // set clip to null in case this child was hidden before (see 'else' block).
               child.Clip = null;
               Grid.SetColumn(child, target_pos);
            } else {
               // hide element by setting clip to 0x0 rect, rather than visibility to collapsed,
               // to avoid incidentally breaking visibility bindings, triggers, animations, etc.
               child.Clip = new RectangleGeometry(new Rect(0, 0, 0, 0));
            }
         }

         public static void reset_sync(DependencyObject d, DependencyPropertyChangedEventArgs e) {
            if (!GetSyncWithColumnGeometry(d))
               SetSyncWithColumnGeometryHook(d, null);
            if (!(d is Grid g))
               return;
            IDisposable hook = null;
            IDisposable hook_wrapper = DisposableFactory.Create(() => DisposableUtils.Dispose(ref hook));
            if (g.IsLoaded) {
               hook = impl();
            }
            SetSyncWithColumnGeometryHook(d, DisposableFactory.Create(g.SubscribeLoaded(on_loaded), hook_wrapper));
            //
            void on_loaded(object sender, RoutedEventArgs loaded_e) {
               if (GetSyncWithColumnGeometry(d)) {
                  hook?.Dispose();
                  hook = impl();
               }
            }
            IDisposable impl() {
               // Avoid resetting children on every collection change, as that could be very noisy and hurt perf.
               // In the visible columns CollectionChanged handler, call reset_children inside a Dispatcher.InvokeAsync request
               // to effectively wait for potential batch-changes to finish.
               bool resetting_children = false;
               if (!(GetSyncWithColumnGeometrySource(d) is DataGrid diy_grid) && !g.try_find_ancestor_of_type(out diy_grid))
                  return null;
               g.SetBinding(Grid.WidthProperty, new Binding(nameof(HeaderTotalSizeHack)) { Source = diy_grid });
               reset_children();
               on_layout_changed(diy_grid.GetColumnGeometry(true).ToList());
               diy_grid.ColumnLayoutChanged += on_layout_changed;
               diy_grid.visible_columns.CollectionChanged += on_vis_columns_changed;
               return DisposableFactory.Create(() => {
                  diy_grid.ColumnLayoutChanged -= on_layout_changed;
                  diy_grid.visible_columns.CollectionChanged -= on_vis_columns_changed;
               });
               //
               void on_vis_columns_changed(object sender, NotifyCollectionChangedEventArgs vce) {
                  if (!resetting_children) {
                     // avoid resetting children on every collection change, as that could be very noisy and hurt perf.
                     resetting_children = true;
                     diy_grid.Dispatcher.InvokeAsync(() => {
                        on_layout_changed(diy_grid.GetColumnGeometry().ToList());
                        reset_children();
                        resetting_children = false;
                     });
                  }
               }
               void on_layout_changed(List<ColumnGeometry> layout) {
                  // TODO: figure out how to make the ColumnLayoutChanged event lazy, or somehow avoid making 2 lists of columns
                  // (the layout param does not include obscured cols, so we have to create a new list of ColumnGeoemtry to satisfy our needs).
                  layout = (from col in diy_grid.visible_columns
                            join pos in diy_grid.GetColumnGeometry(true)
                            on col.ID equals pos.column.ID
                            select pos).ToList();

                  int n_removed = g.ColumnDefinitions.Count - layout.Count;
                  if (n_removed > 0)
                     g.ColumnDefinitions.RemoveRange(g.ColumnDefinitions.Count - n_removed, n_removed);
                  int i = 0;
                  for (; i < g.ColumnDefinitions.Count; i++) {
                     var col_pos = layout[i];
                     var col = g.ColumnDefinitions[i];
                     if (col.Width.GridUnitType == GridUnitType.Pixel && col.Width.Value == col_pos.visible_width)
                        continue;
                     col.Width = new GridLength(col_pos.visible_width, GridUnitType.Pixel);
                  }
                  for (; i < layout.Count; i++) {
                     g.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(layout[i].visible_width, GridUnitType.Pixel) });
                  }
               }
               void reset_children() {
                  foreach (UIElement child in g.Children)
                     reset_panel_child(child, diy_grid);
               }
            }
         }
      }

      static class ColumnResourceHelper {
         public static string col_resource_id(object col_id) =>
            $"Columns_{col_id}";

         public static string col_prop_resource_id(object col_id, string prop) =>
            $"{col_resource_id(col_id)}_{prop}";

         public static void ObservedCachedParamsChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) =>
            reset_cell_resource_references(d as FrameworkElement, e.OldValue as IEnumerable<CachedColumnParam>, e.NewValue as IEnumerable<CachedColumnParam>);

         public static void reset_cell_resource_references(FrameworkElement target, IEnumerable<CachedColumnParam> old_params, IEnumerable<CachedColumnParam> new_params) =>
            reset_cell_resource_references(target, GetColumnKey(target), old_params, new_params);

         public static void reset_cell_resource_references(FrameworkElement target, object col_id, IEnumerable<CachedColumnParam> old_params, IEnumerable<CachedColumnParam> new_params) {
            if (target == null || col_id == null)
               return;
            foreach (var p in old_params.empty_if_null()) {
               target.ClearValue(p.TargetProperty);
            }
            foreach (var p in new_params.empty_if_null()) {
               target.SetResourceReference(p.TargetProperty, col_prop_resource_id(col_id, p.SourcePropertyName));
            }
         }
      }

      class DataContextHooks : IDisposable {
         List<IDisposable> _hooks = new List<IDisposable>(4);

         // this constructor provides a way to avoid repetative dp lookups ala owner.DataContext
         public DataContextHooks(DataGrid owner, object data_context)
            : this(owner, data_context as IHandleRowSelection, data_context as IHandleGridSorting, data_context as IWillLetYouKnowWhenIDispose, data_context as IRequestRowSelection) { }

         public DataContextHooks(DataGrid owner, IHandleRowSelection selection_handler, IHandleGridSorting sorting_handler, IWillLetYouKnowWhenIDispose dispose_broadcaster, IRequestRowSelection selection_request_handler) {
            if (selection_handler != null && owner._row_presenter is ListBox rp) {
               rp.SelectionChanged += on_selection_changed;
               _hooks.Add(DisposableFactory.Create(() => rp.SelectionChanged -= on_selection_changed));
               void on_selection_changed(object sender, SelectionChangedEventArgs e) {
                  if (e.OriginalSource == rp)
                     selection_handler.row_selection_changed(e);
               }
            }
            if (sorting_handler != null) {
               owner.Sorting += sorting_handler.on_sorting;
               _hooks.Add(DisposableFactory.Create(() => owner.Sorting -= sorting_handler.on_sorting));
            }
            if (dispose_broadcaster != null)
               _hooks.Add(dispose_broadcaster.SubscribeDisposed(this));
            if (selection_request_handler != null && owner._row_presenter is DataGridRowsPresenter rpres) {
               selection_request_handler.request_selection += selection_requested;
               _hooks.Add(DisposableFactory.Create(() => selection_request_handler.request_selection -= selection_requested));
               void selection_requested((IEnumerable items, bool is_selected) items_and_selectionstate) {
                  if (items_and_selectionstate.is_selected) {
                     rpres.SetSelectedItems(items_and_selectionstate.items);
                  } else {
                     rpres.UnselectAll();
                  }
               }
            }
         }

         public void Dispose() =>
            DisposableUtils.DisposeItems(_hooks);
      }
   }
}
