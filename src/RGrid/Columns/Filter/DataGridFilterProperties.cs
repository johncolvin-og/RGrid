using RGrid.Filters;
using RGrid.Utility;
using System.Windows;
using System.Windows.Input;

namespace RGrid.Controls {
	static class DataGridFilterProperties {
      #region Commands
      public static readonly RoutedCommand
         CommitCommand = new RoutedCommand("Commit", typeof(DataGridFilterProperties)),
         ClearCommand = new RoutedCommand("Clear", typeof(DataGridFilterProperties));

      public static readonly ExecutedRoutedEventHandler commit_executed = (s, e) => {
         e.Handled = true;
         (e.Parameter as IDataGridColumnFilter)?.Apply.ExecuteIfCan(null);
      };

      public static readonly CanExecuteRoutedEventHandler commit_can_execute = (s, e) => {
         e.Handled = true;
         e.CanExecute = e.Parameter is IDataGridColumnFilter f && f.Apply.CanExecute(null);
      };

      public static readonly ExecutedRoutedEventHandler clear_executed = (s, e) => {
         e.Handled = true;
         (e.Parameter as IDataGridColumnFilter)?.Clear.ExecuteIfCan(null);
      };

      public static readonly CanExecuteRoutedEventHandler clear_can_execute = (s, e) => {
         e.Handled = true;
         e.CanExecute = e.Parameter is IDataGridColumnFilter f && f.Clear.CanExecute(null);
      };
      #endregion

      #region FilterTemplate
      public static readonly DependencyProperty FilterTemplateProperty = DependencyProperty.RegisterAttached("FilterTemplate", typeof(DataTemplate), typeof(DataGridFilterProperties));
      public static DataTemplate GetFilterTemplate(DependencyObject d) { return d.GetValue(FilterTemplateProperty) as DataTemplate; }
      public static void SetFilterTemplate(DependencyObject d, DataTemplate value) { d.SetValue(FilterTemplateProperty, value); }
		#endregion

		#region Filter
		public static readonly DependencyProperty FilterProperty = DependencyProperty.RegisterAttached("Filter", typeof(object), typeof(DataGridFilterProperties));
      public static object GetFilter(DependencyObject d) { return d.GetValue(FilterProperty) as object; }
      public static void SetFilter(DependencyObject d, object value) { d.SetValue(FilterProperty, value); }
      #endregion

      #region FilterActive
      public static readonly DependencyProperty FilterActiveProperty = DependencyProperty.RegisterAttached("FilterActive", typeof(bool?), typeof(DataGridFilterProperties), new PropertyMetadata(false));
      public static bool? GetFilterActive(DependencyObject d) => d.GetValue(FilterActiveProperty) as bool?;
      public static void SetFilterActive(DependencyObject d, bool? value) => d.SetValue(FilterActiveProperty, value);
      #endregion

      #region FilterOpen
      public static readonly DependencyProperty FilterOpenProperty = DependencyProperty.RegisterAttached(
         "FilterOpen", typeof(bool), typeof(DataGridFilterProperties), new FrameworkPropertyMetadata(false, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault));
      public static bool GetFilterOpen(DependencyObject d) { return (bool)d.GetValue(FilterOpenProperty); }
      public static void SetFilterOpen(DependencyObject d, bool value) { d.SetValue(FilterOpenProperty, value); }
      #endregion
   }
}