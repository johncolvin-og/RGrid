using System;
using System.Windows.Markup;

namespace RGrid.WPF {
   [MarkupExtensionReturnType(typeof(int))]
   class IntExtension : MarkupExtension {
      public IntExtension() : this (0) { }
      public IntExtension(int Num) =>
         this.Num = Num;

      [ConstructorArgument("Num")]
      public int Num { get; set; }

      public override object ProvideValue(IServiceProvider serviceProvider) => Num;
   }
}