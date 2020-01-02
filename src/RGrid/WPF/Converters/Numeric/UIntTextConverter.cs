using System;
using System.Globalization;
using System.Windows.Controls;
using System.Windows.Data;

namespace RGrid.WPF.Converters {
	class UIntTextConverter : IValueConverter {
		public object Convert(object value, Type targetType, object parameter, CultureInfo culture) {
			if (!(value is uint)) return new ValidationResult(false, null);
			else return value.ToString();
		}

		public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) {
			var text = value as string;
			if (string.IsNullOrWhiteSpace(text) || !uint.TryParse(text, out uint result)) return new ValidationResult(false, null);
			else return result;
		}
	}

   [ValueConversion(typeof(uint?), typeof(string))]
	class NullableUIntTextConverter : IValueConverter {
		public object Convert(object value, Type targetType, object parameter, CultureInfo culture) =>
			value?.ToString();

		public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) {
			var text = value as string;
			if (string.IsNullOrWhiteSpace(text) || !uint.TryParse(text, out uint result)) return null;
			else return result;
		}
	}

   [ValueConversion(typeof(uint?), typeof(uint))]
   class NullableUIntConverter : IValueConverter {
      public uint DefaultValue { get; set; } = 0u;

      public object Convert(object value, Type targetType, object parameter, CultureInfo culture) {
         var nu = value as uint?;
         return nu.HasValue ? nu.Value : DefaultValue;
      }

      public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) =>
         value is uint ? new uint?((uint)value) : null;
   }
}