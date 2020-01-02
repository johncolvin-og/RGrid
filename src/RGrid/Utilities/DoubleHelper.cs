using System.Linq;

namespace RGrid.Utility {
   public static class DoubleHelper {
      public static bool IsNaNOrInfinity(double d) => double.IsNaN(d) || double.IsInfinity(d);
      public static bool IsNaNOrInfinityOr(double d, params double[] values) => IsNaNOrInfinity(d) || values.Contains(d);
      public static bool IsOneOf(double d, params double[] values) => values.Contains(d);
      public static double CoerceNaN(double value) => CoerceNaN(value, 0.0);
      public static double CoerceNaN(double value, double if_nan) => double.IsNaN(value) ? if_nan : value;
      public static double CoerceNaNOrInfinity(double value) => CoerceNaNOrInfinity(value, 0.0);
      public static double CoerceNaNOrInfinity(double value, double if_nan_or_infinity) => IsNaNOrInfinity(value) ? if_nan_or_infinity : value;
      public static double MultiplyIfNotNaN(double value, double mult) => double.IsNaN(mult) ? value : value * mult;
      public static uint safe_convert_to_uint(double value) {
         switch (value) {
            case double.PositiveInfinity: return uint.MaxValue;
            case double.NegativeInfinity:
            case double.NaN: return 0;
            default:
               if (value < 0) {
                  return 0;
               } else if (value > uint.MaxValue) {
                  return uint.MaxValue;
               }
               return (uint)value;
         }
      }
   }
}