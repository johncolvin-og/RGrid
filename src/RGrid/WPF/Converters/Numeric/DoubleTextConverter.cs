using System;
using System.Globalization;
using System.Windows.Controls;
using System.Windows.Data;

namespace RGrid.WPF.Converters {
   [ValueConversion(typeof(double), typeof(string))]
   class DoubleTextConverter : IValueConverter {
      public const string FORMAT_WITH_COMMAS = "#,0.#############";

      public DoubleTextConverter() {
         Minimum = Double.NaN;
         Maximum = Double.NaN;
         Multiplier = Double.NaN;
      }

      public double Minimum { get; set; }
      public double Maximum { get; set; }
      public double Multiplier { get; set; }
      public string Format { get; set; }
      public string NaNText { get; set; } = string.Empty;

      public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture) {
         var text = (value as string ?? string.Empty).Trim().ToLower();
         if (text == string.Empty || text == "nan" || text == NaNText) return double.NaN;
         if (double.TryParse(text, out double d) && !double.IsNaN(d)) {
            if ((!double.IsNaN(Minimum) && d < Minimum) || (!double.IsNaN(Maximum) && d > Maximum)) return new ValidationResult(false, null);
            return double.IsNaN(Multiplier) ? d : d / Multiplier;
         }
         return new ValidationResult(false, null);
      }

      public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture) {
         if (value is double) {
            double d = (double)value;
            if (!double.IsNaN(d)) {
               if (!double.IsNaN(Multiplier)) d *= Multiplier;
               return string.IsNullOrEmpty(Format) ? d.ToString() : d.ToString(Format);
            }
         }
         return NaNText;
      }
   }

   [ValueConversion(typeof(double?), typeof(string))]
   class NullableDoubleTextConverter : IValueConverter {
      public bool HideNaN { get; set; } = true;

      public object Convert(object value, Type targetType, object parameter, CultureInfo culture) {
         var nd = value as double?;
         return nd.HasValue ?
            (double.IsNaN(nd.Value) && HideNaN) ?
               string.Empty : nd.Value.ToString() :
            string.Empty;
      }
      public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) {
         var text = value as string;
         if (string.IsNullOrWhiteSpace(text) || !double.TryParse(text, out double result)) return null;
         else return result;
      }
   }

   [ValueConversion(typeof(double?), typeof(double))]
   class NullableDoubleConverter : IValueConverter {
      public double DefaultValue { get; set; } = double.NaN;

      public object Convert(object value, Type targetType, object parameter, CultureInfo culture) {
         double? nd = value as double?;
         return nd.HasValue ? nd.Value : DefaultValue;
      }

      public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) =>
         value is double d ? new double?(d) : null;
   }
}