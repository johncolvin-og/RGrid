using Disposable.Extensions;
using System;

namespace RGrid.Utility {
   class DeferrableActionWrapper {
      readonly Action _action;
      bool _is_execution_pending;
      int _deferment_count;

      public DeferrableActionWrapper(Action action) =>
         _action = action ?? throw new ArgumentNullException(nameof(action));

      public bool is_execution_deferred => _deferment_count > 0;

      public void cancel_pending_execution() {
         _is_execution_pending = false;
         _deferment_count = 0;
      }

      public void execute() {
         if (is_execution_deferred) {
            _is_execution_pending = true;
         } else {
            _is_execution_pending = false;
            _action();
         }
      }

      public IDisposable supress_execution() {
         _deferment_count++;
         return DisposableFactory.Create(() => _deferment_count--);
      }

      public IDisposable defer_execution() {
         _deferment_count++;
         return DisposableFactory.Create(() => {
            _deferment_count--;
            if (_is_execution_pending && !is_execution_deferred) {
               _is_execution_pending = false;
               _action();
            }
         });
      }
   }
}