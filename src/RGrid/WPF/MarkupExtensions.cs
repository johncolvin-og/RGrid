using System;
using System.Windows;
using System.Windows.Markup;

namespace RGrid.WPF {
   static class MarkupExtensionExtensions {
      public static IProvideValueTarget GetValueTargetProvider(this IServiceProvider serviceProvider) {
         return serviceProvider.GetService(typeof(IProvideValueTarget)) as IProvideValueTarget;
      }
      public static DependencyObject GetTargetObj(this IServiceProvider serviceProvider) {
         return serviceProvider.GetTargetObj<DependencyObject>();
      }
      public static T GetTargetObj<T>(this IServiceProvider serviceProvider) where T : class {
         object rv = serviceProvider.GetValueTargetProvider().TargetObject;
         return rv as T;
      }
      public static DependencyProperty GetTargetProperty(this IServiceProvider serviceProvider) {
         return serviceProvider.GetValueTargetProvider().TargetProperty as DependencyProperty;
      }
   }
}
