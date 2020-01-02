using System;
using System.Globalization;
using System.Windows.Data;

namespace RGrid.WPF.Converters {
   [ValueConversion(typeof(double), typeof(string))]
   class DoubleMultiplierConverter : IValueConverter {
      public double Multiplier { get; set; }

      public object Convert(object value, Type targetType, object parameter, CultureInfo culture) =>
         value is double d ?  d * Multiplier :
         value is int i ?  i * Multiplier :
         value is uint u ? u * Multiplier :
         double.NaN;

      public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) =>
         value is double d ? d / Multiplier :
         value is int i ?  (double)i / Multiplier :
         value is uint u ? (double)u / Multiplier :
         double.NaN;
   }
}