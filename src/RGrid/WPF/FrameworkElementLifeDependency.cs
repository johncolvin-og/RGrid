using Disposable.Extensions.Utilities;
using RGrid.Utility;
using System;
using System.Reactive.Linq;
using System.Windows;
using System.Windows.Data;

namespace RGrid.WPF {
   abstract class FrameworkElementLifeDependency : IDisposable {
      protected readonly FrameworkElement Target;
      bool _initialized, _disposed;

      protected FrameworkElementLifeDependency(FrameworkElement target) =>
         Target = target;

      public void Initialize() {
         _check_initialized();
         _check_disposed();
         _initialized = true;
         Target.Loaded += _on_target_loaded;
         Target.Unloaded += _on_target_unloaded;
         OnInitialized();
      }

      public void Dispose() {
         _check_disposed();
         _disposed = true;
         if (_initialized) {
            Target.Loaded -= _on_target_loaded;
            Target.Unloaded -= _on_target_unloaded;
            OnDisposed();
         }
      }

      protected virtual void OnInitialized() { }
      protected virtual void OnLoaded() { }
      protected virtual void OnUnloaded() { }
      protected virtual void OnDisposed() { }

      void _check_initialized() { if (_initialized) throw new InvalidOperationException($"{GetType()} is already initialized."); }
      void _check_disposed() { if (_disposed) throw new ObjectDisposedException(GetType().ToString()); }

      void _on_target_loaded(object sender, RoutedEventArgs e) { OnLoaded(); }
      void _on_target_unloaded(object sender, RoutedEventArgs e) { OnUnloaded(); }
   }

   abstract class FrameworkElementLifeDependency<T> : FrameworkElementLifeDependency where T : FrameworkElement {
      protected readonly new T Target;

      protected FrameworkElementLifeDependency(T target) : base(target) =>
         Target = target;
   }

   class FrameworkElementLifeObservableDependency<T> : FrameworkElementLifeDependency where T : class {
      readonly DependencyProperty _target_property;
      readonly IObservable<T> _source;
      IDisposable _sub;
      T _last_value;

      public FrameworkElementLifeObservableDependency(FrameworkElement target, DependencyProperty target_property, IObservable<T> source)
         : base(target) {
         _target_property = target_property;
         _source = source;
      }

      protected override void OnInitialized() { if (Target.IsLoaded) _subscribe(); }
      protected override void OnLoaded() => _subscribe();
      protected override void OnUnloaded() => DisposableUtils.Dispose(ref _sub);
      protected override void OnDisposed() => DisposableUtils.Dispose(ref _sub);

      void _subscribe() {
         DisposableUtils.Dispose(ref _sub);
         _sub = _source.ObserveOnDispatcher().Subscribe(_on_data);
      }
      void _on_data(T data) => Target.SetValue(_target_property, _last_value = data);
   }

   //class FrameworkElementLifeStrongReferenceDependency : FrameworkElementLifeDependency {
   //   public FrameworkElementLifeStrongReferenceDependency(FrameworkElement target, DependencyProperty target_property, EventHandler property_changed_callback)
   //      : base(target) {
   //      _target_property = target_property;
   //      _property_changed_callback = property_changed_callback;
   //   }

   //   private readonly DependencyProperty _target_property;
   //   private readonly EventHandler _property_changed_callback;
   //   private IDisposable _strong_ref;

   //   private void _subscribe() {
   //      Disposable.dispose(ref _strong_ref);
   //      _strong_ref = Target.ObserveProperty(_target_property, _property_changed_callback);
   //   }

   //   protected override void OnInitialized() { if (Target.IsLoaded) _subscribe(); }
   //   protected override void OnLoaded() { _subscribe(); }
   //   protected override void OnUnloaded() { Disposable.dispose(ref _strong_ref); }
   //   protected override void OnDisposed() { Disposable.dispose(ref _strong_ref); }
   //}

   //internal class FrameworkElementLifeStrongPropertyDependency : FrameworkElementLifeDependency
   //{
   //   public FrameworkElementLifeStrongPropertyDependency(FrameworkElement target, DependencyProperty property, EventHandler on_property_changed)
   //      : base(target) {
   //      _property = property;
   //      _on_property_changed = on_property_changed;
   //   }

   //   private readonly DependencyProperty _property;
   //   private readonly EventHandler _on_property_changed;
   //   private IDisposable _property_ref;

   //   protected override void OnLoaded() {
   //      Disposable.dispose(ref _property_ref);
   //      _property_ref = Target.ObserveProperty(_property, _on_property_changed);
   //   }
   //   protected override void OnUnloaded() { Disposable.dispose(ref _property_ref); }
   //   protected override void OnDisposed() { Disposable.dispose(ref _property_ref); }
   //}

   internal class ConverterTargetDependency<TConverter, TInput> : FrameworkElementLifeDependency where TConverter : IValueConverter
   {
      public ConverterTargetDependency(FrameworkElement target, Action<TConverter, TInput> update, TInput initial_value)
         : base(target) {
         _update = update;
         _input = initial_value;
      }

      private readonly Action<TConverter, TInput> _update;
      private BindingExpression _binding_expression;
      private TConverter _converter;

      private TInput _input;
      public TInput Input { get { return _input; } set { _input = value; _update_target(); } }

      private void _update_target() {
         if (_binding_expression == null || _converter == null) return;
         _update(_converter, _input);
         _binding_expression.UpdateTarget();
      }

      protected override void OnLoaded() {
         _binding_expression = TextElementHelper.GetTextBindingExpression(Target);
         if (_binding_expression != null && _binding_expression.ParentBinding != null) _converter = (TConverter)_binding_expression.ParentBinding.Converter;
         _update_target();
      } 
   }

   public static class FrameworkElementLifeDependencyHelper
   {
      public static void DisposeOldInitializeNew(DependencyObject d, DependencyPropertyChangedEventArgs e) {
         var old_val = e.OldValue as FrameworkElementLifeDependency;
         if (old_val != null) old_val.Dispose();
         var new_val = e.NewValue as FrameworkElementLifeDependency;
         if (new_val != null) new_val.Initialize();
      }

      public static void DisposeOld(DependencyObject d, DependencyPropertyChangedEventArgs e) {
         var old_val = e.OldValue as IDisposable;
         if (old_val != null) old_val.Dispose();
      }
   }
}