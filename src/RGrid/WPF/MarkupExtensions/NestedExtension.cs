using System;
using System.Reflection;
using System.Windows.Markup;

namespace RGrid.WPF {
   [MarkupExtensionReturnType(typeof(Type))]
   class NestedExtension : MarkupExtension {
      public NestedExtension() { }

      public NestedExtension(string Type) =>
         this.Type = Type;

      public string Type { get; set; }

      public override object ProvideValue(IServiceProvider serviceProvider) {
         var xtr = serviceProvider.GetService(typeof(IXamlTypeResolver)) as IXamlTypeResolver;
         if (xtr == null)
            return this;
         var split = Type.Split('.');
         var type = xtr.Resolve(split[0]);
         for (int i = 1; i < split.Length; i++)
            type = type.GetNestedType(split[i], BindingFlags.Public | BindingFlags.NonPublic);
         return type;
      }
   }
}