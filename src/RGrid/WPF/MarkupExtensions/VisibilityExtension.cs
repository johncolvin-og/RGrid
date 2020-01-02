using System;
using System.Windows;
using System.Windows.Markup;

namespace RGrid.WPF {
   [MarkupExtensionReturnType(typeof(Visibility))]
   public class VisibilityExtension : MarkupExtension {
      public VisibilityExtension() { }
      public VisibilityExtension(Visibility Visibility) =>
         this.Visibility = Visibility;

      [ConstructorArgument(nameof(Visibility))]
      public Visibility Visibility { get; set; }

      public override object ProvideValue(IServiceProvider serviceProvider) =>
         Visibility;
   }
}