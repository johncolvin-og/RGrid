using RGrid.WPF;
using System.Windows;
using System.Windows.Controls;

namespace RGrid {
   public class DataGridRow : ListBoxItem {
      ContentPresenter _presenter;

      static DataGridRow() =>
         DefaultStyleKeyProperty.OverrideMetadata(typeof(DataGridRow), new FrameworkPropertyMetadata(typeof(DataGridRow)));

      public static readonly DependencyProperty ShowDetailsProperty =
         WPFHelper.create_dp<bool, DataGridRow>(
            nameof(ShowDetails),
            (o, v) => o._update_content_template());

      public bool ShowDetails {
         get => (bool)GetValue(ShowDetailsProperty);
         set => SetValue(ShowDetailsProperty, value);
      }

      public static readonly DependencyProperty DetailsContentProperty =
         WPFHelper.create_dp<object, DataGridRow>(nameof(DetailsContent));

      public object DetailsContent {
         get => GetValue(DetailsContentProperty);
         set => SetValue(DetailsContentProperty, value);
      }

      public static readonly DependencyProperty DetailsContentTemplateProperty =
         WPFHelper.create_dp<DataTemplate, DataGridRow>(
            nameof(DetailsContentTemplate),
            (o, v) => o._update_content_template());

      public DataTemplate DetailsContentTemplate {
         get => GetValue(DetailsContentTemplateProperty) as DataTemplate;
         set => SetValue(DetailsContentTemplateProperty, value);
      }

      public static readonly DependencyProperty DetailsContentTemplateSelectorProperty =
         WPFHelper.create_dp<DataTemplateSelector, DataGridRow>(
            nameof(DetailsContentTemplateSelector),
            (o, v) => o._update_content_template());

      public DataTemplateSelector DetailsContentTemplateSelector {
         get => GetValue(DetailsContentTemplateSelectorProperty) as DataTemplateSelector;
         set => SetValue(DetailsContentTemplateSelectorProperty, value);
      }

      public override void OnApplyTemplate() {
         _presenter = GetTemplateChild("_presenter") as ContentPresenter;
         _update_content_template();
      }

      void _update_content_template() {
         if(_presenter != null) {
            if (ShowDetails) {
               if (DetailsContentTemplate != null) {
                  _presenter.SetCurrentValue(ContentPresenter.ContentTemplateProperty, DetailsContentTemplate);
               } else if (DetailsContentTemplateSelector != null) {
                  _presenter.SetCurrentValue(ContentPresenter.ContentTemplateSelectorProperty, DetailsContentTemplateSelector);
               }
            } else {
               _presenter.ClearValue(ContentPresenter.ContentTemplateProperty);
               _presenter.ClearValue(ContentPresenter.ContentTemplateSelectorProperty);
            }
         }
      }
   }
}