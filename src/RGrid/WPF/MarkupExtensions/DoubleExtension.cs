using System;
using System.ComponentModel;
using System.Windows.Markup;

namespace RGrid.WPF {
   [MarkupExtensionReturnType(typeof(double))]
   class DoubleExtension : MarkupExtension {
      public DoubleExtension() : this(double.NaN) { }

      public DoubleExtension(double Value) =>
         this.Value = Value;

      [ConstructorArgument("Value")]
      [TypeConverter(typeof(DoubleStringTypeConverter))]
      public double Value { get; set; }

      public override object ProvideValue(IServiceProvider serviceProvider) {
         return Value;
      }
   }

   class DoubleStringTypeConverter : TypeConverter {
      public override bool CanConvertFrom(ITypeDescriptorContext context, Type sourceType) { return sourceType == typeof(string); }
      public override bool CanConvertTo(ITypeDescriptorContext context, Type destinationType) { return destinationType == typeof(string); }
      public override object ConvertFrom(ITypeDescriptorContext context, System.Globalization.CultureInfo culture, object value) =>
         double.TryParse(value as string, out double result) ? result : double.NaN;

      public override object ConvertTo(ITypeDescriptorContext context, System.Globalization.CultureInfo culture, object value, Type destinationType) {
         return value == null ? "NULL" : value.ToString();
      }
   }
}
