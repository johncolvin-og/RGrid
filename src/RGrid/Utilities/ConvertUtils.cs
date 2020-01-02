using RGrid.Utility;

namespace System {
   static class ConvertUtils {
      public static object try_convert(Type type, object obj) {
         try_convert(type, obj, out object result);
         return result;
      }

      public static bool try_convert(Type type, object obj, out object result) {
         if (obj == null) {
            result = get_default(type);
            return false;
         } else if (type.IsPrimitive && obj is IConvertible) {
            result = Convert.ChangeType(obj, type);
            return true;
         } else if (type.IsAssignableFrom(obj.GetType())) {
            result = obj;
            return true;
         } else if (type.IsEnum && obj is string) {
            result = EnumHelper.try_parse(type, obj.ToString());
            return true;
         } else if (type.IsNullable()) {
            return try_convert(type.GetGenericArguments()[0], obj, out result);
         } else {
            result = get_default(type);
            return false;
         }
      }

      public static T try_convert<T>(object obj) =>
         (T)try_convert(typeof(T), obj);

      public static bool try_convert<T>(object obj, out T result) {
         bool rv = try_convert(typeof(T), obj, out object r_obj);
         result = (T)r_obj;
         return rv;
      }

      public static object get_default(Type type) =>
         type.IsValueType ? Activator.CreateInstance(type) : null;

      public static T ValueOrDefault<T>(T? nullable) where T : struct => nullable.HasValue ? nullable.Value : default(T);

      public static bool is_compatible<T>(object obj) { return obj is T || (obj == null && default(T) == null); }

      public static bool try_convert_args<T1, T2>(object[] args, out T1 arg1, out T2 arg2) {
         if (args == null || args.Length != 2) {
            arg1 = default(T1);
            arg2 = default(T2);
            return false;
         }
         bool rv = try_convert(args[0], out arg1);
         rv |= try_convert(args[1], out arg2);
         return rv;
      }

      public static bool try_convert_args<T1, T2, T3>(object[] args, out T1 arg1, out T2 arg2, out T3 arg3) {
         if (args == null || args.Length != 3) {
            arg1 = default(T1);
            arg2 = default(T2);
            arg3 = default(T3);
            return false;
         }
         bool rv = try_convert(args[0], out arg1);
         rv |= try_convert(args[1], out arg2);
         rv |= try_convert(args[2], out arg3);
         return rv;
      }

      public static bool try_convert_args<T1, T2, T3, T4>(object[] args, out T1 arg1, out T2 arg2, out T3 arg3, out T4 arg4) {
         if (args == null || args.Length != 4) {
            arg1 = default(T1);
            arg2 = default(T2);
            arg3 = default(T3);
            arg4 = default(T4);
            return false;
         }
         bool rv = try_convert(args[0], out arg1);
         rv |= try_convert(args[1], out arg2);
         rv |= try_convert(args[2], out arg3);
         rv |= try_convert(args[3], out arg4);
         return rv;
      }

      public static bool update_value_type<T>(ref T field, T newval) where T : struct, IEquatable<T> {
         bool rv = field.Equals(newval);
         field = newval;
         return rv;
      }
   }
}