using System;
using System.Globalization;
using System.Windows.Controls;
using System.Windows.Data;

namespace RGrid.WPF.Converters {
	class IntTextConverter : IValueConverter {
		public object Convert(object value, Type targetType, object parameter, CultureInfo culture) {
			if (!(value is int)) return new ValidationResult(false, null);
			else return value.ToString();
		}

		public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) {
			var text = value as string;
			if (string.IsNullOrWhiteSpace(text) || !int.TryParse(text, out int result)) return new ValidationResult(false, null);
			else return result;
		}
	}

   [ValueConversion(typeof(int?), typeof(string))]
	class NullableIntTextConverter : IValueConverter {
		public object Convert(object value, Type targetType, object parameter, CultureInfo culture) =>
			value?.ToString();

		public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) {
			var text = value as string;
			if (string.IsNullOrWhiteSpace(text) || !int.TryParse(text, out int result)) return null;
			else return result;
		}
	}

   [ValueConversion(typeof(int?), typeof(int))]
   class NullableIntConverter : IValueConverter {
      public int DefaultValue { get; set; } = 0;

      public object Convert(object value, Type targetType, object parameter, CultureInfo culture) {
         var ni = value as int?;
         return ni.HasValue ? ni.Value : DefaultValue;
      }

      public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) =>
         value is int ? new int?((int)value) : null;
   }
}