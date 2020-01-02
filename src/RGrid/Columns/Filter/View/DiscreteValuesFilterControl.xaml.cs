using RGrid.Utility;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace mbts.ui.spark
{
   /// <summary>
   /// Interaction logic for DiscreteValuesFilterControl.xaml
   /// </summary>
   internal class DiscreteValuesFilterControl : Control
   {
      static DiscreteValuesFilterControl() {
         DefaultStyleKeyProperty.OverrideMetadata(typeof(DiscreteValuesFilterControl), new FrameworkPropertyMetadata(typeof(DiscreteValuesFilterControl)));
      }

      public static readonly DependencyProperty ActiveProperty = DependencyProperty.Register(nameof(Active), typeof(bool), typeof(DiscreteValuesFilterControl), new PropertyMetadata(false));
      public bool Active { get => (bool)GetValue(ActiveProperty); set => SetValue(ActiveProperty, value); }

      public static readonly DependencyProperty ItemsSourceProperty = DependencyProperty.Register(nameof(ItemsSource), typeof(IEnumerable), typeof(DiscreteValuesFilterControl), new PropertyMetadata(Enumerable.Empty<object>()));
      public IEnumerable ItemsSource { get => GetValue(ItemsSourceProperty) as IEnumerable; set => SetValue(ItemsSourceProperty, value); }

      public static readonly DependencyProperty SelectedItemsProperty = DependencyProperty.Register(nameof(SelectedItems), typeof(IObservableCollectionEx), typeof(DiscreteValuesFilterControl));
      public IObservableCollectionEx SelectedItems { get => GetValue(SelectedItemsProperty) as IObservableCollectionEx; set => SetValue(SelectedItemsProperty, value); }

		//private IDisposable _

		public override void OnApplyTemplate() {
			//var listbox = 
		}
	}
}