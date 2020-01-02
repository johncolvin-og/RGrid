using System;
using System.Linq;
using System.Windows.Data;

namespace RGrid.WPF.Converters {
   class AggregateMultiValueConverter<T> : IMultiValueConverter {
      private  Func<T,object,T> _aggregation_function;

      public AggregateMultiValueConverter(Func<T, object, T> aggregation_function) {
         _aggregation_function = aggregation_function;
      }

      public object Convert(object[] values, Type targetType, object parameter, System.Globalization.CultureInfo culture) {
         return values.Aggregate<object, T>(default(T), _aggregation_function);
      }

      public object[] ConvertBack(object value, Type[] targetTypes, object parameter, System.Globalization.CultureInfo culture) {
         throw new NotImplementedException();
      }
   }

   class SumConverter : IMultiValueConverter {
      static SumConverter s_inst;
      public static SumConverter instance {
         get {
            if (s_inst == null) s_inst = new SumConverter();
            return s_inst;
         }
      }
      public object Convert(object[] values, Type targetType, object parameter, System.Globalization.CultureInfo culture) {
         double total = 0.0;
         foreach (var value in values) {
            if (value is double) {
               total += (double)value;
            }
         }
         return total;
      }

      public object[] ConvertBack(object value, Type[] targetTypes, object parameter, System.Globalization.CultureInfo culture) {
         throw new NotImplementedException();
      }
   }
}