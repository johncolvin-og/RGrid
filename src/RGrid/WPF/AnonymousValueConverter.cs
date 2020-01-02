using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace RGrid.WPF.Converters {
   static class ValueConverter {
      public static IValueConverter create<TIn, TOut>(Func<TIn, TOut> convert) =>
         new AnonymousValueConverter<TIn, TOut>(convert);

      public static IMultiValueConverter create<TIn1, TIn2, TOut>(Func<TIn1, TIn2, TOut> convert) =>
         new AnonymousMultiValueConverter<TIn1, TIn2, TOut>(convert);

      public static IValueConverter BooleanHiddenVisibilityConverter =>
         create<bool, Visibility>(b => b ? Visibility.Visible : Visibility.Hidden);

   }

   public class AnonymousValueConverter : IValueConverter {
      public AnonymousValueConverter(Func<object, object> convert) : this(convert, null) { }
      public AnonymousValueConverter(Func<object, object> convert, Func<object, object> convert_back) {
         _convert = convert;
         _convert_back = convert_back;
      }

      private readonly Func<object, object> _convert, _convert_back;

      public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture) { return _convert(value); }
      public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture) { return _convert_back(value); }
   }

   public class AnonymousValueConverter<TIn, TOut> : IValueConverter {
      public AnonymousValueConverter(Func<TIn, TOut> convert) : this(convert, null) { }
      public AnonymousValueConverter(Func<TIn, TOut> convert, Func<TOut, TIn> convert_back) {
         _convert = convert;
         _convert_back = convert_back;
      }

      private readonly Func<TIn, TOut> _convert;
      private readonly Func<TOut, TIn> _convert_back;

      public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture) { return _convert(ConvertUtils.try_convert<TIn>(value)); }
      public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture) { return _convert_back(ConvertUtils.try_convert<TOut>(value)); }
   }

   public class AnonymousMultiValueConverter<TIn1, TIn2, TOut> : IMultiValueConverter {
      readonly Func<TIn1, TIn2, TOut> _convert;

      public AnonymousMultiValueConverter(Func<TIn1, TIn2, TOut> convert) => _convert = convert;

      public TOut ErrorValue { get; set; }

      public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture) =>
         ConvertUtils.try_convert_args(values, out TIn1 a, out TIn2 b) ? _convert(a, b) : ErrorValue;

      public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture) => throw new NotImplementedException();
   }
}