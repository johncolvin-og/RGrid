using System;
using System.Windows.Input;

namespace RGrid.WPF {
   interface IRaiseCanExecuteChangedCommand : ICommand {
      void RaiseCanExecuteChanged();
   }

   class DelegateCommand<T> : ICommand, IRaiseCanExecuteChangedCommand {
      protected Func<T, bool> _can_execute;
      protected Action<T> _execute;

      protected DelegateCommand() { }
      public DelegateCommand(Action<T> execute, Func<T, bool> can_execute) {
         _execute = execute;
         _can_execute = can_execute;
      }

      public DelegateCommand(Action<T> execute) =>
         _execute = execute;

      public void RaiseCanExecuteChanged() =>
         CanExecuteChanged?.Invoke(this, null);

      public bool CanExecute(object parameter) {
         if (_can_execute == null)
            return true;
         else
            return _can_execute(ConvertUtils.try_convert<T>(parameter));
      }

      public event EventHandler CanExecuteChanged;

      public void Execute(object parameter) =>
         _execute(ConvertUtils.try_convert<T>(parameter));
   }

   class DelegateCommand : DelegateCommand<object> {
      public DelegateCommand(Action execute) : this(_ => execute()) { }
		public DelegateCommand(Action execute, Func<bool> can_execute) : this(_ => execute(), _ => can_execute()) { }
      public DelegateCommand(Action<object> execute) : base(execute) { }
		public DelegateCommand(Action<object> execute, Func<object, bool> can_execute) : base(execute, can_execute) { }
   }
}