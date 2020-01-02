using Disposable.Extensions;
using RGrid.WPF;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reactive.Linq;
using System.Reflection;

namespace RGrid.Utility {
   interface IPropertyChangedDetector<T> where T : IRaiseNotifyPropertyChanged {
      IDisposable detect_changes(T target);
   }

   static class ObjectSnapshot {
      public static IReadOnlyList<Func<TSource, TValue>> property_delegates_of_type<TSource, TValue>() =>
         PropertyGetterStore<TSource, TValue>.value;

      public static IObservable<TableUpdate<(string name, object current, object original), string>> property_table<T>(T instance, params string[] prop_names) where T : INotifyPropertyChanged =>
         _property_table(instance, Array.ConvertAll(prop_names, PropertyInfoStore<T>.type.GetProperty));

      public static IObservable<TableUpdate<(string name, object current, object original), string>> property_table(INotifyPropertyChanged instance, params string[] prop_names) =>
         _property_table(instance, Array.ConvertAll(prop_names, instance.GetType().GetProperty));

      static IObservable<TableUpdate<(string name, object current, object original), string>> _property_table(INotifyPropertyChanged instance, params PropertyInfo[] props) =>
         Observable.Create<TableUpdate<(string name, object current, object original), string>>(o => {
            var cache = props.ToDictionary(p => p.Name, p => new PropNode(p, p.GetValue(instance)));
            var table = TableUpdate<(string name, object current, object original), string>.mk_empty(t => t.name).with_added(cache.Values.Select(pn => (pn.prop.Name, pn.current, pn.original)));
            o.OnNext(table);
            instance.PropertyChanged += on_prop_changed;
            return DisposableFactory.Create(() => instance.PropertyChanged -= on_prop_changed);
            void on_prop_changed(object sender, PropertyChangedEventArgs e) {
               if (cache.TryGetValue(e.PropertyName, out PropNode pn)) {
                  object oldval = pn.current;
                  pn.current = pn.prop.GetValue(instance);
                  table = table.with_changes(new[] { new Change<(string, object, object)>((pn.prop.Name, oldval, pn.original), (pn.prop.Name, pn.current, pn.original)) });
                  o.OnNext(table);
               }
            }
         });

      public static IPropertyChangedDetector<T> build_detector<T>(params string[] props) where T : IRaiseNotifyPropertyChanged =>
         new DiscretePropertyChangedDetector<T>(props);

      public static IPropertyChangedDetector<T> build_detector_except<T>(params string[] except_props) where T : IRaiseNotifyPropertyChanged {
         // convert to a hashset for quicker lookups
         var exclude = new HashSet<string>(except_props);
         return new DiscretePropertyChangedDetector<T>(PropertyInfoStore<T>.all_props.Where(p => !exclude.Contains(p.Name)).Select(p => p.Name).ToArray());
      }

      public static IObservable<bool> dirty_monitor<T>(T instance, params string[] prop_names) where T : INotifyPropertyChanged =>
         dirty_monitor(instance, Array.ConvertAll(prop_names, PropertyInfoStore<T>.type.GetProperty));

      public static IObservable<bool> dirty_monitor<T>(T instance, out IDisposable snapshot, params string[] prop_names) where T : INotifyPropertyChanged =>
         dirty_monitor(instance, out snapshot, Array.ConvertAll(prop_names, PropertyInfoStore<T>.type.GetProperty));

      public static IObservable<bool> dirty_monitor<T>(T instance, out IDisposable snapshot, params PropertyInfo[] props) where T : INotifyPropertyChanged {
         var prop_map = props.ToDictionary(p => p.Name, p => new PropNode(p, p.GetValue(instance)));
         snapshot = DisposableFactory.Create(() => {
            foreach (var pn in prop_map.Values)
               pn.prop.SetValue(instance, pn.original);
         });
         return _dirty_monitor(instance, prop_map);
      }

      public static IObservable<bool> dirty_monitor<T>(T instance, PropertyInfo[] props) where T : INotifyPropertyChanged =>
         _dirty_monitor(instance, props.ToDictionary(p => p.Name, p => new PropNode(p, p.GetValue(instance))));

      static IObservable<bool> _dirty_monitor<T>(T instance, Dictionary<string, PropNode> prop_map) where T : INotifyPropertyChanged =>
         Observable.Create<bool>(o => {
            var dirty_props = new HashSet<string>();
            instance.PropertyChanged += on_prop_changed;
            return DisposableFactory.Create(() => instance.PropertyChanged -= on_prop_changed);
            void on_prop_changed(object sender, PropertyChangedEventArgs e) {
               if (prop_map.TryGetValue(e.PropertyName, out var p)) {
                  p.current = p.prop.GetValue(instance);
                  if (Equals(p.current, p.original) == p.is_dirty) {
                     p.is_dirty = !p.is_dirty;
                     if (p.is_dirty) {
                        if (dirty_props.Add(p.prop.Name) && dirty_props.Count == 1)
                           o.OnNext(true);
                     } else if (dirty_props.Remove(p.prop.Name) && dirty_props.Count == 0)
                        o.OnNext(false);
                  }
               }
            }
         });

      public static IDisposable create<T>(T instance) =>
         create(instance, PropertyInfoStore<T>.mutable_props);

      public static IDisposable create<T>(T instance, PropertyInfo[] props) {
         var snapshot = Array.ConvertAll(props, p => p.GetValue(instance));
         return DisposableFactory.Create(() => {
            for (int i = 0; i < props.Length; i++)
               props[i].SetValue(instance, snapshot[i]);
         });
      }

      static Func<T, TValue> _prop_getter<T, TValue>(PropertyInfo prop) =>
         (Func<T, TValue>)prop.GetGetMethod().CreateDelegate(typeof(Func<T, TValue>));

      static class PropertyInfoStore<T> {
         public static readonly Type type = typeof(T);

         public static readonly PropertyInfo[]
            all_props = type.GetProperties(BindingFlags.Public | BindingFlags.Instance),
            mutable_props;

         static PropertyInfoStore() =>
            mutable_props = all_props.Where(p => p.GetGetMethod() != null).ToArray();
      }

      // DelegateStore maintains a cache of property getter/setter delegates,
      // which perform much better than the corresponding PropertyInfo.GetValue/SetValue.
      static class DelegateStore<T> {
         public static readonly Type type = typeof(T);

         static readonly IReadOnlyDictionary<string, (Func<T, object> get, Action<T, object> set)> _property_delegates;

         public static Func<T, object> getter(string prop_name) =>
            _property_delegates.TryGetValue(prop_name, out var dels) ? dels.get : null;

         public static Action<T, object> setter(string prop_name) =>
            _property_delegates.TryGetValue(prop_name, out var dels) ? dels.set : null;

         static DelegateStore() {
            var prop_dels = new Dictionary<string, (Func<T, object> get, Action<T, object> set)>();
            foreach (var p in PropertyInfoStore<T>.all_props) {
               Func<T, object> wrapped_getter = null;
               Action<T, object> wrapped_setter = null;
               var getter = p.GetGetMethod();
               var setter = p.GetSetMethod();
               if (getter == null && setter == null)
                  continue;
               var del_types = (typeof(Func<,>).MakeGenericType(typeof(T), p.PropertyType), typeof(Action<,>).MakeGenericType(typeof(T), p.PropertyType));
               var twrapper = typeof(Wrapper<>).MakeGenericType(typeof(T), p.PropertyType);
               if (getter != null) {
                  wrapped_getter = (Func<T, object>)
                     twrapper.GetMethod(nameof(Wrapper<T>.getter), BindingFlags.Static | BindingFlags.Public)
                     .Invoke(null, new[] { getter.CreateDelegate(del_types.Item1) });
               }
               if (setter != null) {
                  wrapped_setter = (Action<T, object>)
                     twrapper.GetMethod(nameof(Wrapper<T>.setter), BindingFlags.Static | BindingFlags.Public)
                     .Invoke(null, new[] { setter.CreateDelegate(del_types.Item2) });
               }
               prop_dels[p.Name] = (wrapped_getter, wrapped_setter);
            }
            _property_delegates = prop_dels;
         }

         static class Wrapper<TValue> {
            public static Func<T, object> getter(Func<T, TValue> getter) => src => getter(src);
            public static Action<T, object> setter(Action<T, TValue> setter) => (src, val) => setter(src, (TValue)val);
         }
      }

      static class PropertyGetterStore<TSource, TValue> {
         public static readonly IReadOnlyList<Func<TSource, TValue>> value;

         static PropertyGetterStore() {
            var tval = typeof(TValue);
            value = typeof(TSource).GetProperties(BindingFlags.Public | BindingFlags.Instance)
               .Where(p => tval.IsAssignableFrom(p.PropertyType))
               .Select(_prop_getter<TSource, TValue>)
               .ToReadOnlyList();
         }
      }

      class PropNode {
         public readonly PropertyInfo prop;

         public PropNode(PropertyInfo prop, object init) {
            this.prop = prop;
            original = init;
            current = init;
         }

         public bool is_dirty { get; set; }
         public object original { get; set; }
         public object current { get; set; }
      }

      class DiscretePropertyChangedDetector<T> : IPropertyChangedDetector<T> where T : IRaiseNotifyPropertyChanged {
         static readonly Converter<string, (string name, Func<T, object> get)> _realize_prop =
            (prop_name) => (prop_name, DelegateStore<T>.getter(prop_name));

         readonly (string name, Func<T, object> get)[] _target_props;

         public DiscretePropertyChangedDetector()
            : this(Array.ConvertAll(PropertyInfoStore<T>.all_props, p => p.Name)) { }

         public DiscretePropertyChangedDetector(params string[] props) =>
            _target_props = Array.ConvertAll(props, _realize_prop);

         public IDisposable detect_changes(T target) {
            var get_target_val = new Converter<(string name, Func<T, object> get), object>(tup => tup.get(target));
            var orig_state = Array.ConvertAll(_target_props, get_target_val);
            return DisposableFactory.Create(() => {
               var curr_state = Array.ConvertAll(_target_props, get_target_val);
               for (int i = 0; i < curr_state.Length; i++) {
                  if (!Equals(curr_state[i], orig_state[i]))
                     target.raise_notify_property_changed(_target_props[i].name);
               }
            });
         }
      }
   }
}