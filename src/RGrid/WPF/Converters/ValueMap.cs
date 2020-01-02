using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Data;
using System.Windows.Markup;

namespace RGrid.WPF.Converters {
   [ContentProperty("Map")]
   class ValueMap : IValueConverter {
      public ValueMap() =>
         Map = new List<object>();
      
      public IList Map { get; set; }
      public object Default { get; set; }

      public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture) {
         foreach (var kv in Map.Cast<KeyValue>())
            if (Equals(kv.Key, value))
               return kv.Value;
         return Default;
      }

      public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture) {
         foreach (var kv in Map.Cast<KeyValue>())
            if (Equals(kv.Key, value))
               return kv.Key;
         return Default;
      }
   }

   class KeyValue {
      public object Key { get; set; }
      public object Value { get; set; }
   }
}