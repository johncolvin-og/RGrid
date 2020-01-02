using System;
using System.Windows.Markup;
using System.Xaml;

namespace RGrid.WPF {
   class DirectReferenceExtension : MarkupExtension {
		public DirectReferenceExtension() { }
		public DirectReferenceExtension(string Name) { this.Name = Name; }
		[ConstructorArgument(nameof(Name))]
		public string Name { get; set; }
		//IRootObjectProvider
		//IValueSerializerContext
		//ITypeDescriptorContext
		//IXamlTypeResolver
		//IUriContext
		//IAmbientProvider
		//IXamlNamespaceResolver
		//IProvideValueTarget
		//IXamlNameResolver
		//IDestinationTypeProvider
		public override object ProvideValue(IServiceProvider serviceProvider) {
			var xnr = serviceProvider.GetService(typeof(IXamlNameResolver)) as IXamlNameResolver;
			if (xnr != null) {
				foreach (var kv in xnr.GetAllNamesAndValuesInScope()) {
					if (kv.Key == Name) return kv.Value;
				}
			}
			return this;
		}
	}
}