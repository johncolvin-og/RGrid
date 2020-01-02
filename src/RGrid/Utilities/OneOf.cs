using EqualityComparer.Extensions;
using System;
using System.Collections.Generic;

namespace RGrid.Utility {
   static class OneOf {
      public static OneOf<A, B> create<A, B>(Func<A> a, Func<B> b) where A : class where B : class {
         if (a() is A as_a) {
            return new OneOf<A, B>(as_a);
         } else if (b() is B as_b) {
            return new OneOf<A, B>(as_b);
         } else return new OneOf<A, B>();
      }

      public static OneOf<A, B, C> create<A, B, C>(Func<A> a, Func<B> b, Func<C> c) where A : class where B : class where C : class {
         if (a() is A as_a) {
            return new OneOf<A, B, C>(as_a);
         } else if (b() is B as_b) {
            return new OneOf<A, B, C>(as_b);
         } else if (c() is C as_c) {
            return new OneOf<A, B, C>(as_c);
         } else return new OneOf<A, B, C>();
      }

      public static void Match<A, B, C>(Func<A> a, Func<B> b, Func<C> c, Action<A> on_a, Action<B> on_b, Action<C> on_c) {
         if (a() is A as_a) {
            on_a(as_a);
         } else if (b() is B as_b) {
            on_b(as_b);
         } else if (c() is C as_c) {
            on_c(as_c);
         }
      }

      public static void Match<A, B, C, D>(Func<A> a, Func<B> b, Func<C> c, Func<D> d, Action<A> on_a, Action<B> on_b, Action<C> on_c, Action<D> on_d) {
         if (a() is A as_a) {
            on_a(as_a);
         } else if (b() is B as_b) {
            on_b(as_b);
         } else if (c() is C as_c) {
            on_c(as_c);
         } else if (d() is D as_d) {
            on_d(as_d);
         }
      }

      public static OneOf<A, B> MatchFirst<TSource, A, B>(IEnumerable<TSource> source) {
         foreach (var v in source) {
            if (v is A va) {
               return new OneOf<A, B>(va);
            } else if (v is B vb)
               return new OneOf<A, B>(vb);
         }
         return new OneOf<A, B>();
      }

      public static T infer<T, A, B>(object value, Func<A, T> a, Func<B, T> b) =>
         OneOf<A, B>.infer(value).Match(a, b);
   }

   public class OneOf<A, B> {
      public static readonly IEqualityComparer<OneOf<A, B>> ValueEqualityComparer =
         EqualityComparerFactory.Create<OneOf<A, B>, object>(oneof => oneof.value).ToSafe();

      int _tag;
      readonly A _a;
      readonly B _b;
      public OneOf() => _tag = -1;
      public OneOf(A a) { _a = a; _tag = 0; }
      public OneOf(B b) { _b = b; _tag = 1; }

      public static OneOf<A, B> infer(object obj) {
         if (obj is A a) return new OneOf<A, B>(a);
         if (obj is B b) return new OneOf<A, B>(b);
         return new OneOf<A, B>();
      }

      public void Match(Action<A> a, Action<B> b) {
         switch (_tag) {
            case 0: a(_a); break;
            case 1: b(_b); break;
         }
      }

      public T Match<T>(Func<A, T> a, Func<B, T> b) {
         switch (_tag) {
            case 0: return a(_a);
            case 1: return b(_b);
            default: return default(T);
         }
      }

      public object value {
         get {
            switch (_tag) {
               case 0: return _a;
               case 1: return _b;
               default: return null;
            }
         }
      }
   }

   public class OneOf<A, B, C> {
      int _tag;
      readonly A _a;
      readonly B _b;
      readonly C _c;
      public OneOf() => _tag = -1;
      public OneOf(A a) { _a = a; _tag = 0; }
      public OneOf(B b) { _b = b; _tag = 1; }
      public OneOf(C c) { _c = c; _tag = 2; }

      public static OneOf<A, B, C> infer(object obj) {
         if (obj is A a) return new OneOf<A, B, C>(a);
         if (obj is B b) return new OneOf<A, B, C>(b);
         if (obj is C c) return new OneOf<A, B, C>(c);
         return new OneOf<A, B, C>();
      }

      public void Match(Action<A> a, Action<B> b, Action<C> c) {
         switch (_tag) {
            case 0: a(_a); break;
            case 1: b(_b); break;
            case 2: c(_c); break;
         }
      }

      public T Match<T>(Func<A, T> a, Func<B, T> b, Func<C, T> c) {
         switch (_tag) {
            case 0: return a(_a);
            case 1: return b(_b);
            case 2: return c(_c);
            default: return default(T);
         }
      }

      public OneOf<A2, B2, C2> Extend<A2, B2, C2>(Func<A, A2> a, Func<B, B2> b, Func<C, C2> c) {
         switch (_tag) {
            case 0: return new OneOf<A2, B2, C2>(a(_a));
            case 1: return new OneOf<A2, B2, C2>(b(_b));
            case 2: return new OneOf<A2, B2, C2>(c(_c));
            default: return null;
         }
      }

      public object value {
         get {
            switch (_tag) {
               case 0: return _a;
               case 1: return _b;
               case 2: return _b;
               default: return null;
            }
         }
      }
   }

   public class OneOf<A, B, C, D> {
      int _tag;
      readonly A _a;
      readonly B _b;
      readonly C _c;
      readonly D _d;
      public OneOf(A a) { _a = a; _tag = 0; }
      public OneOf(B b) { _b = b; _tag = 1; }
      public OneOf(C c) { _c = c; _tag = 2; }
      public OneOf(D d) { _d = d; _tag = 3; }

      public void ActOn(Action<A> a, Action<B> b, Action<C> c, Action<D> d) {
         switch (_tag) {
            case 0: a(_a); break;
            case 1: b(_b); break;
            case 2: c(_c); break;
            case 4: d(_d); break;
         }
      }

      public T Match<T>(Func<A, T> a, Func<B, T> b, Func<C, T> c, Func<D, T> d) {
         switch (_tag) {
            case 0: return a(_a);
            case 1: return b(_b);
            case 2: return c(_c);
            case 4: return d(_d);
            default: return default(T);
         }
      }

      public object value {
         get {
            switch (_tag) {
               case 0: return _a;
               case 1: return _b;
               case 2: return _b;
               case 4: return _d;
               default: return null;
            }
         }
      }
   }
}
