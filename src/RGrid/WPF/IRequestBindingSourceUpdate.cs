using Disposable.Extensions;
using Disposable.Extensions.Utilities;
using RGrid.Utility;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Windows;
using System.Windows.Data;
using System.Windows.Markup;

namespace RGrid.WPF {
   interface IRequestBindingSourceUpdate {
      event Action RequestUpdate;
   }

   class BindingUpdateGroupCollection : List<BindingUpdateGroup> { }

   [ContentProperty(nameof(Properties))]
   class BindingUpdateGroup : DependencyObject {
      IDisposable _hook;
      List<DependencyProperty> _properties;

      public BindingUpdateGroup() =>
         Properties = new List<object>();

      public static readonly DependencyProperty TargetProperty = DependencyProperty.Register(
         nameof(Target), typeof(FrameworkElement), typeof(BindingUpdateGroup));

      public FrameworkElement Target { get => GetValue(TargetProperty) as FrameworkElement; set => SetValue(TargetProperty, value); }

      public static readonly DependencyProperty SourceProperty = DependencyProperty.Register(
         nameof(Source), typeof(IRequestBindingSourceUpdate), typeof(BindingUpdateGroup), new PropertyMetadata(null, OnSourceChanged));

      static void OnSourceChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) =>
         ((BindingUpdateGroup)d)._reset();

      public IRequestBindingSourceUpdate Source { get => GetValue(SourceProperty) as IRequestBindingSourceUpdate; set => SetValue(SourceProperty, value); }

      public static readonly DependencyProperty PropertiesProperty = DependencyProperty.Register(
         nameof(Properties), typeof(IList), typeof(BindingUpdateGroup), new PropertyMetadata(OnPropertiesChanged));

      static void OnPropertiesChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) =>
         ((BindingUpdateGroup)d)._properties = null;

      public IList Properties { get => GetValue(PropertiesProperty) as IList; set => SetValue(PropertiesProperty, value); }

      void _reset() {
         DisposableUtils.Dispose(ref _hook);
         var source = Source;
         if (source != null) {
            source.RequestUpdate += _source_RequestUpdate;
            _hook = DisposableFactory.Create(() => source.RequestUpdate -= _source_RequestUpdate);
         }
      }

      void _source_RequestUpdate() {
         var tgt = Target;
         if (tgt != null)
            if (_properties == null) {
               var pts = Properties;
               if (pts == null) return;
               _properties = new List<DependencyProperty>();
               foreach (object p in pts) {
                  if (p is DependencyProperty dp)
                     _properties.Add(dp);
                  else if (p is PropertyTuple pt)
                     _properties.Add(pt.get_property());
               }
            }
         foreach (DependencyProperty dp in _properties)
            if (tgt.GetBindingExpression(dp) is BindingExpression be)
               be.UpdateSource();
      }
   }

   class PropertyTuple {
      public PropertyTuple() { }
      public PropertyTuple(string propertyName, Type type) {
         PropertyName = propertyName;
         Type = type;
      }

      public string PropertyName { get; set; }
      public Type Type { get; set; }

      public DependencyProperty get_property() {
         ExceptionAssert.InvalidOperation.PropertyNotNull((PropertyName, nameof(PropertyName)), (Type, nameof(Type)));
         var field = Type.GetField(PropertyName + "Property", BindingFlags.Static | BindingFlags.Public);
         if (field != null) return field.GetValue(null) as DependencyProperty;
         return null;
      }
   }
}