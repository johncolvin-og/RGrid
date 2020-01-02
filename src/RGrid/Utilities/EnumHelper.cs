using RGrid.Utility;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace RGrid.Utility {
   static class EnumHelper {
      public static IEnumerable<T> get_values<T>() { return Enum.GetValues(typeof(T)).Cast<T>(); }

      public static IEnumerable<T> get_values_except<T>(params T[] except) { return get_values<T>().Except(except); }

      public static IEnumerable get_values(Type t) {
         if (!t.IsEnum) {
            if (t.IsNullable()) t = Nullable.GetUnderlyingType(t);
            if (!t.IsEnum) return Enumerable.Empty<object>();
         }
         return Enum.GetValues(t);
      }

      public static T parse<T>(string name) where T : struct /*(Enum, more specifically)*/ {
         if (!typeof(T).IsEnum) throw new InvalidOperationException($"{typeof(T)} is not an enum type.");
         if (!Enum.TryParse(name, true, out T result)) throw new ArgumentException(nameof(name), "Does not match any of the values");
         return result;
      }

      public static T parse_or_default<T>(string value) where T : struct /*(Enum, more specifically)*/ =>
         typeof(T).IsEnum && Enum.TryParse(value, true, out T result) ? result : default(T);

      public static object try_parse(Type enum_type, string value, bool ignore_case = true) {
         ExceptionAssert.Argument.must_be_so(enum_type.IsEnum, "an enum type", nameof(enum_type));
         Array vals = Enum.GetValues(enum_type);
         var str_comp = ignore_case ? StringComparison.CurrentCultureIgnoreCase : StringComparison.CurrentCulture;
         foreach (object v in vals) {
            if (string.Equals(v.ToString(), value, str_comp))
               return v;
         }
         return Activator.CreateInstance(enum_type);
      }

      public static TAttribute get_attribute<TEnum, TAttribute>(TEnum value) where TEnum : struct where TAttribute : Attribute {
         string str = value.ToString();
         MemberInfo[] mi = typeof(TEnum).GetMember(str);
         return mi.Length > 0 ? mi[0].GetCustomAttribute<TAttribute>() : null;
      }

      // though it isn't hard to write "flags & TEnum.Foo != 0," split_flags offers a less error-prone alternative in terms of typeos.
      // (less likely that code calling this method will have a typeo, than code checking flags in the traditional manner described above)
      public static IEnumerable<TEnum> split_flags<TEnum>(TEnum flags) where TEnum : struct {
         long lflags = Convert.ToInt64(flags);
         foreach (TEnum e in get_values<TEnum>()) {
            long lval = Convert.ToInt64(e);
            if ((lval & lflags)!= 0)
               yield return e;
         }
      }
   }
}