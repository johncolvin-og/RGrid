using Disposable.Extensions;
using System;
using System.Collections.Generic;
using System.Reactive.Subjects;

namespace RGrid.Utility {
   public class DistinctSubject<T> : ISubject<T> {
      readonly object _lock;
      readonly IEqualityComparer<T> _comparer;
      T _value;

      public DistinctSubject()
         : this(false) { }

      public DistinctSubject(bool is_synchronized)
         : this(is_synchronized, EqualityComparer<T>.Default) { }

      public DistinctSubject(bool is_synchronized, IEqualityComparer<T> comparer) {
         if (is_synchronized)
            _lock = new object();
         _comparer = comparer;
      }

      public event Action<T> ValueChanged;

      public bool IsSynchronized => _lock != null;

      public T Value {
         get {
            if (IsSynchronized) {
               lock (_lock)
                  return _value;
            }
            return _value;
         }
         set {
            if (IsSynchronized) {
               lock (_lock) {
                  if (!_comparer.Equals(_value, value)) {
                     _value = value;
                     ValueChanged?.Invoke(value);
                  }
               }
            } else if (!_comparer.Equals(_value, value)) {
               _value = value;
               ValueChanged?.Invoke(value);
            }
         }
      }

      public void OnCompleted() { }

      public void OnError(Exception error) { }

      public void OnNext(T value) =>
         Value = value;

      public IDisposable Subscribe(IObserver<T> observer) {
         if (IsSynchronized) {
            lock (_lock)
               ValueChanged += observer.OnNext;
            return DisposableFactory.Create(() => {
               lock (_lock)
                  ValueChanged -= observer.OnNext;
            });
         } else {
            ValueChanged += observer.OnNext;
            return DisposableFactory.Create(() => ValueChanged -= observer.OnNext);
         }
      }
   }
}
