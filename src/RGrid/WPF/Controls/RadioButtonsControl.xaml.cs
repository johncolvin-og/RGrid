using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace RGrid.Controls {
   /// <summary>
   /// Interaction logic for RadioButtonsControl.xaml
   /// </summary>
   public partial class RadioButtonsControl : Selector
   {
      static RadioButtonsControl() {
         DefaultStyleKeyProperty.OverrideMetadata(typeof(RadioButtonsControl), new FrameworkPropertyMetadata(typeof(RadioButtonsControl)));
      }

      public static readonly DependencyProperty OrientationProperty = DependencyProperty.Register("Orientation", typeof(Orientation), typeof(RadioButtonsControl), new FrameworkPropertyMetadata(Orientation.Vertical));
      public Orientation Orientation { get { return (Orientation)GetValue(OrientationProperty); } set { SetValue(OrientationProperty, value); } }

      protected override DependencyObject GetContainerForItemOverride() {
         var item = new RadioButtonItem();
         item.Selected += _item_Selected;
         return item;
      }

      private void _item_Selected(object sender, RoutedEventArgs e) {
         var item = sender as RadioButtonItem;
         if (item != null)
            SelectedItem = item.DataContext;
      }
   }

   public class RadioButtonItem : RadioButton
   {
      static RadioButtonItem() {
         DefaultStyleKeyProperty.OverrideMetadata(typeof(RadioButtonItem), new FrameworkPropertyMetadata(typeof(RadioButtonItem)));
         IsCheckedProperty.OverrideMetadata(typeof(RadioButtonItem), new FrameworkPropertyMetadata(false, FrameworkPropertyMetadataOptions.NotDataBindable, _is_checked_changed));
         IsSelectedProperty = Selector.IsSelectedProperty.AddOwner(typeof(RadioButtonItem), new FrameworkPropertyMetadata(false, _is_selected_changed));
         SelectedEvent = Selector.SelectedEvent.AddOwner(typeof(RadioButtonItem));
         UnselectedEvent = Selector.UnselectedEvent.AddOwner(typeof(RadioButtonItem));
      }

      static void _is_checked_changed(DependencyObject d, DependencyPropertyChangedEventArgs e) {
         var target = (RadioButtonItem)d;
         if (e.NewValue is bool)
            target.IsSelected = (bool)e.NewValue;
         else if (e.NewValue is bool? && ((bool?)e.NewValue).HasValue)
             target.IsSelected = ((bool?)e.NewValue).Value;
         else
            target.IsSelected = false;
      }

      static void _is_selected_changed(DependencyObject d, DependencyPropertyChangedEventArgs e) {
         var target = (RadioButtonItem)d;
         if (target.IsSelected) {
            target.IsChecked = true;
            target.RaiseEvent(new RoutedEventArgs(SelectedEvent, target));
         } else {
            target.IsChecked = false;
            target.RaiseEvent(new RoutedEventArgs(UnselectedEvent, target));
         }
      }

      public static readonly DependencyProperty IsSelectedProperty;
      public bool IsSelected { get { return (bool)GetValue(IsSelectedProperty); } set { SetValue(IsSelectedProperty, value); } }

      public static RoutedEvent SelectedEvent;
      public static RoutedEvent UnselectedEvent;

      public event RoutedEventHandler Selected { add { AddHandler(SelectedEvent, value); } remove { RemoveHandler(SelectedEvent, value); } }
      public event RoutedEventHandler Unselected { add { AddHandler(UnselectedEvent, value); } remove { RemoveHandler(UnselectedEvent, value); } }
   }
}