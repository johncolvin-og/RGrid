using System;
using System.Windows.Controls;
using System.Windows.Data;

namespace RGrid.WPF.Converters
{
   internal class ByteTextConverter : IValueConverter
   {
      public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture) {
         if (!(value is byte)) return new ValidationResult(false, null);
         return value.ToString();
      }

      public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture) {
         var text = value as string;
         if (string.IsNullOrWhiteSpace(text)) return new ValidationResult(false, null);
         if (byte.TryParse(text, out byte result)) return result;
         return new ValidationResult(false, null);
      }
   }
}