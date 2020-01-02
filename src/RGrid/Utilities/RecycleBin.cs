using System;

namespace RGrid.Utility {
   class RecycleBin<T> where T : class {
      readonly Action<T> _init, _destroy;
      T _value;

      public RecycleBin(Action<T> init, Action<T> destroy) {
         _init = init;
         _destroy = destroy;
      }
      public RecycleBin(Func<T, IDisposable> init) {
         IDisposable d = null;
         _init = v => d = init(v);
         _destroy = _ => d.Dispose();
      }

      public T value {
         get => _value;
         set {
            if (!ReferenceEquals(value, _value)) {
               if (_value != null)
                  _destroy?.Invoke(_value);
               _value = value;
               if (_value != null)
                  _init?.Invoke(_value);
            }
         }
      }
   }
}