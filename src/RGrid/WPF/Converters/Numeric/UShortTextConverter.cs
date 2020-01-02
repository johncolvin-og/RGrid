using System;
using System.Globalization;
using System.Windows.Controls;
using System.Windows.Data;

namespace RGrid.WPF.Converters {
	class UShortTextConverter : IValueConverter {
		public object Convert(object value, Type targetType, object parameter, CultureInfo culture) {
			if (!(value is ushort)) return new ValidationResult(false, null);
			else return value.ToString();
		}

		public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) {
			var text = value as string;
			if (string.IsNullOrWhiteSpace(text) || !ushort.TryParse(text, out ushort result)) return new ValidationResult(false, null);
			else return result;
		}
	}

	class NullableUShortTextConverter : IValueConverter {
		public object Convert(object value, Type targetType, object parameter, CultureInfo culture) {
			return value?.ToString();
		}

		public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) {
			var text = value as string;
			if (string.IsNullOrWhiteSpace(text) || !ushort.TryParse(text, out ushort result)) return null;
			else return result;
		}
	}
}