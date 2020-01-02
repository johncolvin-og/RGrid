using RGrid.Utility;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;

namespace RGrid.WPF {
   interface ITemplateSelectorKey {
      string key { get; }
   }
   interface IPropertyWrapperVM<in TMutable, in TImmutable> {
      void assign_to(TMutable value);
      void load_from(TImmutable value);
   }

   interface IPropertyWrapperVM<T> {
      void assign_to(T value);
      void load_from(T value);
   }

   interface IImmutablePropertyWrapperVM<TSource, TValue> {
      TSource update_source(TSource curr, TValue value);
      void load_from(TValue value);
   }

   interface IValueFactory<T> {
      T create();
      void load_from(T value);
   }

   interface IMutableValueWrapperVM<T> : INotifyPropertyChanged {
      T value { get; set; }
   }

   interface IViewValue {
      object value { get; }
      object view { get; }
   }

   interface IViewValue<T> : IViewValue {
      new T value { get; }
   }

   class ViewValueTuple<TValue> : IViewValue<TValue> {
      public ViewValueTuple(TValue value, object view) {
         this.value = value;
         this.view = view;
      }

      public TValue value { get; }
      public object view { get; }
      object IViewValue.value => value;

      public override string ToString() => value.ToString();
   }

   interface ITemplatedPropertyWrapperVM<T> : IPropertyWrapperVM<T>, ITemplateSelectorKey { }

   static class PropertyWrapperVMUtils {
      public static T create_assigned<T>(this IPropertyWrapperVM<T> vm) where T : new() =>
         vm.create_assigned(() => new T());

      public static T create_assigned<T>(this IPropertyWrapperVM<T> vm, Func<T> create_new) {
         var rv = create_new();
         vm.assign_to(rv);
         return rv;
      }

      public static TVM init_vm<T, TVM>(T value) where TVM : IPropertyWrapperVM<T>, new() {
         var vm = new TVM();
         vm.load_from(value);
         return vm;
      }

      public static IMutableValueWrapperVM<T> create_mutable_vm<T>(T init = default(T)) =>
         create_mutable_vm(EqualityComparer<T>.Default, init);

      public static IMutableValueWrapperVM<T> create_mutable_vm<T>(IEqualityComparer<T> comparer, T init = default(T)) =>
         new MutableValueWrapperVM<T>(comparer, init);

      public static ITemplatedPropertyWrapperVM<TSource> create_vm<TSource, TValue>(string template_key, Func<TSource, TValue> get, Action<TSource, TValue> set, Dictionary<string, IObservable<object>> named_params = null) =>
         new DelegatePropertyWrapperVM<TSource, TValue>(template_key, get, set, named_params);

      public static IPropertyWrapperVM<TOutter> link<TOutter, TInner>(this IPropertyWrapperVM<TInner> inner, Func<TOutter, TInner> get, Action<TOutter, TInner> set) =>
         new LinkedPropertyWrapperVM<TOutter, TInner>(inner, get, set);

      class MutableValueWrapperVM<T> : ViewModelBase, IMutableValueWrapperVM<T> {
         readonly IEqualityComparer<T> _comparer;
         readonly Property<T> _value;

         public MutableValueWrapperVM(IEqualityComparer<T> comparer, T init) {
            _comparer = comparer;
            _value = backing(nameof(value), comparer, init);
         }

         public T value { get => _value.Value; set => _value.set(value); }

         public override string ToString() => value?.ToString();
      }

      class LinkedPropertyWrapperVM<TOutter, TInner> : ViewModelBase, IPropertyWrapperVM<TOutter> {
         readonly Func<TOutter, TInner> _get;
         readonly Action<TOutter, TInner> _set;

         public LinkedPropertyWrapperVM(IPropertyWrapperVM<TInner> inner, Func<TOutter, TInner> get, Action<TOutter, TInner> set) {
            _get = get;
            _set = set;
            value = inner;
         }

         public IPropertyWrapperVM<TInner> value { get; }

         public void assign_to(TOutter value) {
            TInner inner_val = _get(value);
            this.value.assign_to(inner_val);
            _set(value, inner_val);
         }
         public void load_from(TOutter value) {
            TInner inner_val = _get(value);
            this.value.load_from(inner_val);
         }
      }

      class DelegatePropertyWrapperVM<T, TValue> : DisposableViewModelBase, ITemplatedPropertyWrapperVM<T> {
         readonly Func<T, TValue> _get;
         readonly Action<T, TValue> _set;
         readonly Property<TValue> _value;
         readonly string _template_key;
         readonly Dictionary<string, object> _named_params = new Dictionary<string, object>();

         public DelegatePropertyWrapperVM(string template_key, Func<T, TValue> get, Action<T, TValue> set, Dictionary<string, IObservable<object>> named_params = null) {
            _template_key = template_key;
            _get = get;
            _set = set;
            _value = backing<TValue>(nameof(value));
            if (!named_params.is_null_or_empty()) {
               // REVIS
               //_dispose = Disposable.Create(named_params.Select(kv =>
               //   kv.Value.OnDispatcher().Subscribe(v => {
               //      _named_params[kv.Key] = v;
               //      // unf the only way to notify the view that an indexed prop has changed
               //      // is to fire PropertyChanged with the hard-coded string below, which causes all indexed property bindings to update.
               //      RaisePropertyChanged("Item[]"); 
               //   })).ToArray());
            }
         }

         public TValue value { get => _value.Value; set => this._value.set(value); }
         public IReadOnlyDictionary<string, object> named_params { get; }
         string ITemplateSelectorKey.key => _template_key;

         //https://stackoverflow.com/questions/657675/propertychanged-for-indexer-property
         [IndexerName("Item")]
         public object this[string name] => _named_params.TryGetValue(name, out object result) ? result : null;

         public void assign_to(T source) => _set(source, value);
         public void load_from(T source) => _value.set(_get(source));
      }

      class ItemsPropertyWrapperVM<T, TValue> : ViewModelBase, ITemplatedPropertyWrapperVM<T> {
         readonly Func<T, IEnumerable<TValue>> _get;
         readonly Action<T, IEnumerable<TValue>> _set;
         readonly string _template_key;

         public ItemsPropertyWrapperVM(string template_key, Func<T, IEnumerable<TValue>> get, Action<T, IEnumerable<TValue>> set) {
            _template_key = template_key;
            _get = get;
            _set = set;
         }

         public ObservableCollection<TValue> value { get; } = new ObservableCollection<TValue>();
         string ITemplateSelectorKey.key => _template_key;

         public void assign_to(T source) => _set(source, value);
         public void load_from(T source) => value.sync_with(_get(source));
      }
   }
}