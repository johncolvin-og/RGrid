using System;
using System.Collections.Generic;
using System.Linq;

namespace RGrid.Utility {
   static class MathUtils {
      public const double epsilon = 1E-12;
      public static bool epsilon_equals(double x, double y) =>
         Math.Abs(x - y) < epsilon;

      public static double GCD(IEnumerable<double> nums) {
         var no_zeros = nums.Where(n => n != 0);
         if (!no_zeros.Any()) return 0;
         double rv = no_zeros.First();
         foreach (var num in no_zeros.Skip(1)) rv = GCD(rv, num);
         return rv;
      }
      public static double GCD(double a, double b) { return (double)GCD((decimal)a, (decimal)b); }
      public static decimal GCD(decimal a, decimal b) {
         decimal small, large;
         if (a < b) {
            small = a;
            large = b;
         } else {
            small = b;
            large = a;
         }
         int i = 1;
         while (i < 9999) {
            decimal rv = small / i++;
            if (large % rv == 0) return rv;
         }
         return 0;
      }
      public static int LCM(IEnumerable<int> nums) {
         var no_zeros = nums.Where(n => n != 0);
         if (!no_zeros.Any()) return 0;
         int rv = no_zeros.First();
         foreach (var num in no_zeros.Skip(1)) rv = LCM(rv, num);
         return rv;
      }
      public static int LCM(int a, int b) {
         int num1, num2;
         if (a > b) {
            num1 = a;
            num2 = b;
         } else {
            num1 = b;
            num2 = a;
         }
         for (int i = 1; i < num2; i++) if ((num1 * i) % num2 == 0) return i * num1;
         return num1 * num2;
      }

      public static bool is_within_range(this double value, double min, double max) =>
         value >= min && value <= max;

      public static int within_range(int min, int max, int value) {
         if (value < min) return min;
         if (value > max) return max;
         return value;
      }
      public static double within_range(double min, double max, double value) {
         if (value < min) return min;
         if (value > max) return max;
         return value;
      }
      public static int RoundFractionalUnits(double value, double fractional_unit) =>
         (int)Math.Round(value / fractional_unit);

      public static bool is_int(double d) =>
         epsilon_equals(d, Math.Round(d));

      public static bool is_int(double d, out double with_int_value) {
         with_int_value = Math.Round(d);
         return epsilon_equals(with_int_value, d);
      }

      public static double factor_out(double value, params double[] factors) {
         foreach (double f in factors)
            value = factor_out(value, f, out int _);
         return value;
      }

      public static double factor_out(double value, double factor, out int n_factors_removed) {
         n_factors_removed = 0;
         if (value == 0)
            return value;
         double quotient = value / factor;
         while (is_int(quotient, out quotient)) {
            value = quotient;
            ++n_factors_removed;
            quotient /= factor;
         }
         return value;
      }

      public static (T min, T max) get_outliers<T>(params T[] values) where T : struct, IComparable<T> {
         if (values.Length == 0) {
            return (default, default);
         }
         switch (values.Length) {
            case 0: return (default, default);
            case 1: return (values[0], values[0]);
            default:
               T min = values[0], max = values[values.Length - 1];
               if (min.CompareTo(max) > 0)
                  _swap(ref min, ref max);
               for (int i = 1; i < values.Length; i++) {
                  if (min.CompareTo(values[i]) > 0) {
                     _swap(ref values[i], ref min);
                  } else if (max.CompareTo(values[i]) < 0) {
                     _swap(ref values[i], ref max);
                  }
               }
               return (min, max);
         }
      }

      static void _swap<T>(ref T a, ref T b) {
         var temp = a;
         a = b;
         b = temp;
      }

      public static class Primes {
         public const int
            P_1087 = 1087,
            P_2423 = 2423,
            P_17191 = 17191,
            P_25097 = 25097,
            P_48247 = 48247,
            P_68909 = 68909,
            P_83987 = 83987,
            P_356219 = 356219,
            P_919223 = 919223,
            P_1276777 = 1276777,
            P_2492003 = 2492003;
      }
   }
}
