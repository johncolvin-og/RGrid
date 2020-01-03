using Disposable.Extensions;
using Disposable.Extensions.Utilities;
using RGrid.Utility;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Linq.Expressions;
using System.Reactive.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows.Input;

namespace RGrid.WPF {
   interface IRaiseNotifyPropertyChanged {
      void raise_notify_property_changed(string prop);
   }

   public class ViewModelBase : INotifyPropertyChanged, IRaiseNotifyPropertyChanged
   {
      public ViewModelBase() {
         _props = new Lazy<PropertyInfo[]>(() => {
            return GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance);
         });
      }
      protected void RaisePropertyChanged([CallerMemberName]string property="") {
         if (PropertyChanged != null) {
            PropertyChanged(this, new PropertyChangedEventArgs(property));
         }
      }

      protected void RaisePropertyChanged(params string[] properties) {
         if (PropertyChanged != null) {
            foreach (var prop in properties) { RaisePropertyChanged(prop); }
         }
      }

      void IRaiseNotifyPropertyChanged.raise_notify_property_changed(string prop) =>
         RaisePropertyChanged(prop);

      protected bool Set<T>(ref T dest, T value, [CallerMemberName]string name = "") =>
         Set(ref dest, value, EqualityComparer<T>.Default, name);

      protected bool Set<T>(ref T dest, T value, IEqualityComparer<T> comparer, [CallerMemberName]string name = "") {
         if (!comparer.Equals(dest, value)) {
            dest = value;
            RaisePropertyChanged(name);
            return true;
         }
         return false;
      }

      public event PropertyChangedEventHandler PropertyChanged;

      private Lazy<PropertyInfo[]> _props;


      #region Object Diffing
      public interface IObjSnapshot : IDisposable {
         bool revert { get; set; }
      }

      public class ObjSnapshot : IObjSnapshot {
         readonly ViewModelBase _target;
         readonly Dictionary<PropertyKeyWrapper, object> _snapshot;
         readonly IReadOnlyList<PropertyInfo> _props;

         public ObjSnapshot(ViewModelBase viewModelBase, IReadOnlyList<PropertyInfo> props) {
            _target = viewModelBase;
            _props = props;
            _snapshot = snap();
         }

         public bool revert { get; set; }

         public void Dispose() {
            var cur = snap();
            foreach (var kv in cur) {
               object oldv = _snapshot[kv.Key];
               object newv = cur[kv.Key];
               if (!Equals(oldv, newv)) {
                  var prop = kv.Key.property;
                  if (revert && prop.CanWrite)
                     prop.SetValue(_target, oldv);
                  _target.RaisePropertyChanged(prop.Name);
               }
            }
         }

         Dictionary<PropertyKeyWrapper, object> snap() =>
            // NOTE: in the future we can optimize prop.GetValue() if needed
            _props.ToDictionary(prop => new PropertyKeyWrapper(prop), prop => prop.GetValue(_target));

         private class PropertyKeyWrapper : IEquatable<PropertyKeyWrapper> {
            public readonly PropertyInfo property;
            public PropertyKeyWrapper(PropertyInfo property) => this.property = property;
            public bool Equals(PropertyKeyWrapper other) => other != null && other.property.Name == property.Name;
            public override bool Equals(object obj) => Equals(obj as PropertyKeyWrapper);
            public override int GetHashCode() => property.Name.GetHashCode();
         }
      }

      // Automatic (but shallow) property change notifications based on local object modifications
      public ObjSnapshot detect_property_changes(params string[] props) {
         if (props.Length == 0) return new ObjSnapshot(this, _props.Value);
         else return new ObjSnapshot(this, _props.Value.Intersect(props, p => p.Name).ToList());
      }

      public ObjSnapshot detect_property_changes_except(params string[] props) {
         if (props.Length == 0) return new ObjSnapshot(this, _props.Value);
         else return new ObjSnapshot(this, _props.Value.Except(props, p => p.Name).ToList());
      }
      #endregion

      #region automatic property
      protected delegate void PropertyChange<T>(T oldval, T newval);

      // use nameof() to add compile time checks
      protected Property<T> backing<T>(string name, T default_value = default(T)) =>
         backing(name, EqualityComparer<T>.Default, default_value);

      protected Property<T> backing<T>(string name, IEqualityComparer<T> comparer, T default_value = default(T)) {
         return new Property<T>(this, name, comparer, default_value);
      }

      protected Property<T> backing<T>(Expression<Func<T>> member, T default_value) {
         var exp = (MemberExpression)member.Body;
         return new Property<T>(this, exp.Member.Name, default_value);
      }

		protected DelegateProperty<T> backing<T>(string name, Func<T> get_value, params string[] invalidating_props) {
			return new DelegateProperty<T>(this, name, get_value, invalidating_props);
		}

      protected PropertyAutoWrapper<TModel, TView> backing<TModel, TView>(string name, TModel initial_model, Func<TModel, TView> convert) =>
         new PropertyAutoWrapper<TModel, TView>(this, name, initial_model, convert, EqualityComparer<TModel>.Default);

      protected DelegatePropertyAutoWrapper<TModel, TView> backing<TModel, TView>(string name, Func<TModel> get_model, Func<TModel, TView> convert) =>
         new DelegatePropertyAutoWrapper<TModel, TView>(this, name, get_model, convert, EqualityComparer<TModel>.Default);

      protected class Property<T> : IObservable<(T oldval,T newval)> {
         readonly string _propname;
         readonly IEqualityComparer<T> _comparer;
         readonly ViewModelBase _owner;
         T _value;

         public Property(ViewModelBase owner, string name, T default_value)
            : this(owner, name, EqualityComparer<T>.Default, default_value) { }

         public Property(ViewModelBase owner, string name, IEqualityComparer<T> comparer, T default_value) {
            _owner = owner;
            _propname = name;
            _comparer = comparer;
            _value = default_value;
         }

         public event PropertyChange<T> Changing, Changed;

         public string name => _propname;

         public void set(T value) {
            if (!Equals(value, _value)) {
               Changing?.Invoke(_value, value);
               var old = _value;
               _value = value;
               Changed?.Invoke(old, value);
               _owner.RaisePropertyChanged(_propname);
            }
         }

         public IDisposable Subscribe(IObserver<(T oldval, T newval)> observer) {
            Changed += pch;
            return DisposableFactory.Create(() => Changed -= pch);
            void pch(T o, T n) => observer.OnNext((o, n));
         }

         public IObservable<T> observe_current(bool start_immediately = false) {
            var source = this.Select(vals => vals.newval);
            if (start_immediately)
               source = source.StartWith(_value);
            return source;
         }

         public T Value {
            get { return _value; }
            set { set(value); }
         }
      }

		protected class DelegateProperty<T> : Property<T> {
			public DelegateProperty(ViewModelBase owner, string name, Func<T> get_value, params string[] invalidating_props)
            : base(owner, name, get_value()) {
				_get_value = get_value;
            if (invalidating_props != null && invalidating_props.Length > 0) {
               var iprop_set = new HashSet<string>(invalidating_props);
               owner.PropertyChanged += (s, e) => {
                  if (iprop_set.Contains(e.PropertyName))
                     refresh();
               };
            }
			}

         readonly Func<T> _get_value;
			public void refresh() { set(_get_value()); }
		}

      /// <summary>
      /// Inspired by ObservableAutoWrapper, a read/write TModel property is used to create a readonly TView property.
      /// </summary>
      protected class PropertyAutoWrapper<TModel, TView> : IObservable<((TModel model, TView view) oldval, (TModel model, TView view) newval)> {
         readonly ViewModelBase _owner;
         readonly Func<TModel, TView> _convert;
         readonly IEqualityComparer<TModel> _comparer;
         readonly string _prop_name;
         TModel _model;
         TView _view;

         public PropertyAutoWrapper(ViewModelBase owner, string name, TModel initial_value, Func<TModel, TView> convert, IEqualityComparer<TModel> comparer) {
            _owner = owner;
            _prop_name = name;
            _model = initial_value;
            _convert = convert;
            _comparer = comparer;
            _view = convert(_model);
         }

         public event PropertyChange<(TModel model, TView view)> changing, changed;

         public string name => _prop_name;
         public TModel model { get => _model; set => set(value); }
         public TView view => _view;

         public void set(TModel model) {
            if (!_comparer.Equals(model, _model)) {
               TView new_view = _convert(model);
               changing?.Invoke((_model, _view), (model, new_view));
               TView old_view = _view;
               TModel old_model = _model;
               _model = model;
               _view = new_view;
               changed?.Invoke((old_model, old_view), (_model, _view));
               _owner.RaisePropertyChanged(_prop_name);
            }
         }

         public IDisposable Subscribe(IObserver<((TModel model, TView view) oldval, (TModel model, TView view) newval)> observer) {
            changed += pch;
            return DisposableFactory.Create(() => changed -= pch);
            void pch((TModel, TView) o, (TModel, TView) n) => observer.OnNext((o, n));
         }

         public IObservable<(TModel model, TView view)> observe_current(bool start_immediately = false) {
            var source = this.Select(vals => vals.newval);
            if (start_immediately)
               source = source.StartWith((_model, _view));
            return source;
         }
      }

      protected class DelegatePropertyAutoWrapper<TModel, TView> : PropertyAutoWrapper<TModel, TView> {
         readonly Func<TModel> _get_model;

         public DelegatePropertyAutoWrapper(ViewModelBase owner, string name, Func<TModel> get_model, Func<TModel, TView> convert, IEqualityComparer<TModel> comparer)
            : base(owner, name, get_model(), convert, comparer) => _get_model = get_model;

         public void refresh() => set(_get_model());
      }

      /// <summary>
      /// A thin wrapper around Property&lt;T&gt; that trips a 'setter_called' flag when the value is set through value-property-setter (i.e., through User interaction).
      /// <para/>Note: This is class is particularly useful when a default/settings-based value arrives late, and the viewmodel must decide whether or not to override the current value.
      /// </summary>
      protected class UserProperty<T> : IObservable<(T oldval, T newval)> {
         readonly Property<T> _prop_model;

         public UserProperty(Property<T> prop_model) =>
            _prop_model = prop_model;

         public event PropertyChange<T> Changing { add { _prop_model.Changing += value; } remove { _prop_model.Changing -= value; } }
         public event PropertyChange<T> Changed { add { _prop_model.Changed += value; } remove { _prop_model.Changed -= value; } }

         public IDisposable Subscribe(IObserver<(T oldval, T newval)> observer) => _prop_model.Subscribe(observer);
         public IObservable<T> observe_current(bool start_immediately = false) => _prop_model.observe_current(start_immediately);

         public T Value {
            get => _prop_model.Value;
            set {
               setter_called = true;
               _prop_model.set(value);
            }
         }
         public bool setter_called { get; private set; }

         /// <summary>
         /// Sets the value without tripping the 'setter_called' flag.
         /// </summary>
         public void bypass_setter(T value) => _prop_model.set(value);
         /// <summary>
         /// Sets 'setter_called' to false.
         /// </summary>
         public void reset() => setter_called = false;
      }

      #endregion

      protected class ObservablePropertySink<T> : Property<T>, IObserver<T>
      {
         public ObservablePropertySink(ViewModelBase owner, string name, T default_val) : base(owner, name, default_val) { }
         public void OnCompleted() { }
         public void OnError(Exception error) { }

         public void OnNext(T value) { set(value); }
      }
   } 

   public class RequestViewCloseViewModelBase : ViewModelBase, IRequestViewClose
   {
      public void TryClose() {
            RequestClose?.Invoke(this, new EventArgs());
      }

      public event EventHandler RequestClose;
   }

   public static class RequestViewCloseExtensions
   {
      public static ICommand close_command(this RequestViewCloseViewModelBase rvc) {
         return new DelegateCommand(rvc.TryClose);
      }
   }

   public class VMValueWrapper<T> : ViewModelBase where T : IEquatable<T>
   {
      public VMValueWrapper(T val) { _val = val; }
      T _val;
      public T value { get { return _val; } set { if (value == null || !value.Equals(_val)) { _val = value; RaisePropertyChanged(); } } }
   }

   // Adds set_error and clear_error to ViewModelBase
   public class ViewModelBaseWithValidation : ViewModelBase, INotifyDataErrorInfo
   {
      Dictionary<string, string> _errors = new Dictionary<string, string>();

      protected void assert(bool condition, [CallerMemberName]string property = "", string message = null) {
         if (condition) {
            clear_error(property);
         } else if (!_errors.TryGetValue(property, out string result) || !string.Equals(message, result)) {
            _errors[property] = message;
            FireErrorsChanged(property);
         }
      }

      protected bool has_errors(string property) =>
         _errors.ContainsKey(property);

      protected void set_error(string property, string message, bool ignore_if_already_error = false) {
         if (!_errors.ContainsKey(property) || !ignore_if_already_error) {
            _errors[property] = message;
            FireErrorsChanged(property);
         }
      }

      protected void clear_error(string property) {
         if (_errors.ContainsKey(property)) {
            _errors.Remove(property);
            FireErrorsChanged(property);
         }
      }

      protected async Task assert_flash_error(bool test, params string[] properties) {
         if (!test) await flash_error(properties);
         else {
            foreach (var prop in properties)
               clear_error(prop);
         }
      }

      protected async Task flash_error_msg(string msg, TimeSpan duration, params string[] properties) {
         foreach (var prop in properties)
            set_error(prop, msg);
         await Task.Delay(duration);
         foreach (var prop in properties)
            clear_error(prop);
      }

      protected async Task flash_error_msg(string msg, params string[] properties) =>
         await flash_error_msg(msg, TimeSpan.FromSeconds(3), properties);

      protected async Task flash_error(params string[] properties) =>
         await flash_error_msg(string.Empty, properties);

      protected IDisposable validation() {
         IEnumerable<string> current = _errors.Keys.ToList();
         _errors.Clear();
         return _validation(current);
      }

      protected IDisposable validation(params string[] properties) {
         IEnumerable<string> current = _errors.Keys.Intersect(properties).ToList();
         foreach (var p in current) _errors.Remove(p);
         return _validation(current);
      }

      protected void validation_assert(bool test, string messsage, params string[] properties) {
         if (!test) {
            foreach (var prop in properties) {
               set_error(prop, messsage);
            }
         }
      }

      IDisposable _validation(IEnumerable<string> validating_properties) =>
         DisposableFactory.Create(() => {
            foreach (var property in validating_properties)
               if (!_errors.ContainsKey(property))
                  FireErrorsChanged(property);
         });


      void FireErrorsChanged(string property) {
         ErrorsChanged?.Invoke(this, new DataErrorsChangedEventArgs(property));
      }

      #region INotifyDataErrorInfo
      public event EventHandler<DataErrorsChangedEventArgs> ErrorsChanged;

      public System.Collections.IEnumerable GetErrors(string propertyName) {
         if (!string.IsNullOrEmpty(propertyName)) {
            if (_errors.TryGetValue(propertyName, out string error)) {
               yield return error;
            }
         }
      }

      public bool HasErrors {
         get { return _errors.Count != 0; }
      }

      public IEnumerable<string> AllErrors { get { return _errors.Values; } }
      #endregion

      protected Property<T> backing<T>(string name, T default_value, bool validate_now = false, params Func<T, bool>[] validation_steps) =>
          backing(name, default_value, validation_steps.Select(v => new PropertyValidation<T>(v)).ToArray(), validate_now);

      protected Property<T> backing<T>(string name, T default_value, IEnumerable<PropertyValidation<T>> validation_steps, bool validate_now = false) {
         void run_validation(T val) {
            foreach (var pv in validation_steps) {
               (bool valid, string msg) = pv.validate(val);
               assert(valid, name, msg);
            }
         };

         var prop = new Property<T>(this, name, default_value);
         prop.Changed += (old_val, new_val) => run_validation(new_val);
         if (validate_now) {
            run_validation(prop.Value);
         }
         return prop;
      }

      protected UserProperty<T> user_backing<T>(string name, T default_value = default(T)) =>
         new UserProperty<T>(backing(name, default_value));

      protected UserProperty<T> user_backing<T>(Property<T> property) =>
         new UserProperty<T>(property);

      //protected IDisposable observe_property<T>(Property<T> property, Action)


      // TODO: maybe include a revert_on_error flag?
      protected class PropertyValidation<T> {
         public readonly Func<T, (bool valid, string msg)> validate;

         public PropertyValidation(Func<T, bool> validate) =>
            this.validate = val => (validate(val), null);

         public PropertyValidation(Func<T, (bool valid, string msg)> validate) =>
            this.validate = validate;
      }
   }

   public abstract class DisposableViewModelBase : ViewModelBaseWithValidation, IWillLetYouKnowWhenIDispose, IDisposable
   {
      protected IDisposable _dispose = null;
      private List<IDisposable> _observable_properties;

      public event Action<bool> OnDisposed;

      protected Property<T> backing<T>(string name, IObservable<T> source, T default_value = default(T)) {
         if (_observable_properties == null) _observable_properties = new List<IDisposable>();
         var prop = new ObservablePropertySink<T>(this, name, default_value);
         _observable_properties.Add(source.Subscribe(prop));
         return prop;
      }

      /// <summary>
      /// Use this generic overload when the [T]value must be created from the [TData]data on this thread, and a simple observable.Select() call will not work.
      /// <para/>Note: UI settings in particular may find this useful, as brushes and other Freezable types are not inherently thread safe.
      /// </summary>
      protected Property<T> backing<T, TData>(string name, IObservable<TData> source, Func<TData, T> select, T default_value = default(T)) {
         if (_observable_properties == null) _observable_properties = new List<IDisposable>();
         var prop = new ObservablePropertySink<T>(this, name, default_value);
         _observable_properties.Add(source.Subscribe(d => prop.set(select(d))));
         return prop;
      } 

      protected void set_dispose(IDisposable disposable) {
         if (_dispose != null) throw new InvalidOperationException("Cannot set disposable twice");
         _dispose = disposable;
      }
      protected void set_dispose(params IDisposable[] garbage) {
         set_dispose(DisposableFactory.Create(garbage));
      }

      protected virtual void Dispose(bool disposing) { }
      public void Dispose() {
         DisposableUtils.Dispose(ref _dispose);
         DisposableUtils.DisposeAndClear(_observable_properties);
         Dispose(true);
         OnDisposed?.Invoke(true);
      }
   }

   [Browsable(false)]
   [EditorBrowsable(EditorBrowsableState.Never)]
   static class NotifyDataErrorInfoExtensions {
      public static IDisposable subscribe_errors_changed(this INotifyDataErrorInfo self, EventHandler<DataErrorsChangedEventArgs> callback) {
         self.ErrorsChanged += callback;
         return DisposableFactory.Create(() => self.ErrorsChanged -= callback);
      }
   }
}