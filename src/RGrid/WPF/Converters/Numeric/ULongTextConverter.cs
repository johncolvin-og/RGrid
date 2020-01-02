﻿using System;
using System.Globalization;
using System.Windows.Controls;
using System.Windows.Data;

namespace RGrid.WPF.Converters
{
	internal class ULongTextConverter : IValueConverter
	{
		public object Convert(object value, Type targetType, object parameter, CultureInfo culture) {
			if (!(value is ulong)) return new ValidationResult(false, null);
			else return value.ToString();
		}

		public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) {
			var text = value as string;
			if (string.IsNullOrWhiteSpace(text) || !ulong.TryParse(text, out ulong result)) return new ValidationResult(false, null);
			else return result;
		}
	}

	internal class NullableULongTextConverter : IValueConverter
	{
		public object Convert(object value, Type targetType, object parameter, CultureInfo culture) {
			return value?.ToString();
		}

		public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) {
			var text = value as string;
			if (string.IsNullOrWhiteSpace(text) || !ulong.TryParse(text, out ulong result)) return null;
			else return result;
		}
	}
}