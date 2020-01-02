using Disposable.Extensions;
using Disposable.Extensions.Utilities;
using RGrid.WPF;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;

namespace RGrid.Controls {
   class NotifyDataErrorAdorner<TView, TViewModel> : Adorner, IDisposable where TView : Control where TViewModel : class, INotifyDataErrorInfo {
      readonly IList<IChildProperty> _children = new List<IChildProperty>();
      IDisposable _errors_changed_hook;

      protected readonly TView _view;
      protected TViewModel _vm;

      public NotifyDataErrorAdorner(TView view) : base(view) {
         _view = view;
         _vm = _view.DataContext as TViewModel;
         _connect_vm();
         _view.DataContextChanged += _data_context_changed;
      }
 
      public virtual void Dispose() {
         _view.DataContextChanged -= _data_context_changed;
         DisposableUtils.Dispose(ref _errors_changed_hook);
      }

      protected ChildProperty<T> child_property<T>(string name) where T : FrameworkElement {
         var cp = new ChildProperty<T>(_view, name);
         _children.Add(cp);
         return cp;
      }

      void _connect_vm() {
         DisposableUtils.Dispose(ref _errors_changed_hook);
         if (_view.DataContext is TViewModel vm) {
            (_vm = vm).ErrorsChanged += _errors_changed;
            _errors_changed_hook = DisposableFactory.Create(() => {
               vm.ErrorsChanged -= _errors_changed;
               foreach (var cp in _children) cp.recycle();
            });
            InvalidateVisual();
         }
      }

      void _data_context_changed(object sender, DependencyPropertyChangedEventArgs e) =>
         _connect_vm();

      void _errors_changed(object sender, DataErrorsChangedEventArgs e) =>
         InvalidateVisual();

      private interface IChildProperty {
         void recycle();
      }

      protected class ChildProperty<T> : IChildProperty where T : FrameworkElement {
         readonly TView _parent;
         readonly string _name;
         T _value;

         internal ChildProperty(TView parent, string name) {
            _parent = parent;
            _name = name;
         }

         public T value => _value ?? (_value = _parent.assert_template_child<T>(_name));

         void IChildProperty.recycle() => _value = null;
      }
   }
}