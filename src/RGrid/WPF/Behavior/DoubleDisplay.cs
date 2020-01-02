using RGrid.WPF.Converters;
using System.Windows;
using System.Windows.Data;

namespace RGrid.WPF { 
   static class DoubleDisplay {
      #region Multiplier Property

      public static readonly DependencyProperty MultiplierProperty = DependencyProperty.RegisterAttached("Multiplier", typeof(double), typeof(DoubleDisplay), new PropertyMetadata(OnMultiplierChanged));
      private static void OnMultiplierChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) {
         BindingExpression be; Binding binding; DoubleTextConverter converter;
         if (TextElementHelper.try_get_binding_components(d, out be, out binding, out converter)) {
            converter.Multiplier = (double)e.NewValue;
            be.UpdateTarget();
         }
      }
      public static double GetMultiplier(DependencyObject d) { return (double)d.GetValue(MultiplierProperty); }
      public static void SetMultiplier(DependencyObject d, double value) { d.SetValue(MultiplierProperty, value); }

      #endregion

      #region Format Property

      public static readonly DependencyProperty FormatProperty = DependencyProperty.RegisterAttached("Format", typeof(string), typeof(DoubleDisplay), new PropertyMetadata(OnFormatChanged));
      private static void OnFormatChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) {
         BindingExpression be; Binding binding; DoubleTextConverter converter;
         if (TextElementHelper.try_get_binding_components(d, out be, out binding, out converter)) {
            converter.Format = e.NewValue as string;
            be.UpdateTarget();
         }
      }
      public static bool GetFormat(DependencyObject d) { return (bool)d.GetValue(FormatProperty); }
      public static void SetFormat(DependencyObject d, bool value) { d.SetValue(FormatProperty, value); }

      #endregion
   }
}