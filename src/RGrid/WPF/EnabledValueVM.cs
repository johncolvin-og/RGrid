namespace RGrid.WPF {
   public class EnabledValueVM<T> : ViewModelBase {
      readonly Property<bool> _enabled;
      readonly Property<T>_value;

      public EnabledValueVM() {
         _enabled = backing<bool>(nameof(enabled));
         _value = backing<T>(nameof(value));
      }

      public bool enabled { get => _enabled.Value; set => _enabled.set(value); }
      public T value { get => _value.Value; set => _value.set(value); }
   }

   public class NamedValueVM<T> {
      public NamedValueVM(string name, T value) {
         this.name = name;
         this.value = value;
      }

      public T value { get; }
      public string name { get; }
   }
}