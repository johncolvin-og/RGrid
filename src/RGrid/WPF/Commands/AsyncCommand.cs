using RGrid.Utility;
using System;
using System.ComponentModel;
using System.Threading.Tasks;
using System.Windows.Input;

namespace RGrid.WPF {
   interface IAsyncCommand : ICommand {
      new Task Execute(object parameter);
      bool is_executing { get; }
      bool allow_concurrent_executions { get; }
   }

   class AsyncCommand<T> : IAsyncCommand, IRaiseCanExecuteChangedCommand, INotifyPropertyChanged {
      readonly Func<T, Task> _execute;
      readonly Func<T, bool> _can_execute;
      bool _allow_concurrent_executions = false;
      int _n_executing = 0;

      public AsyncCommand(Func<T, Task> execute, bool allow_concurrent_executions = false)
         : this(execute, _ => true, allow_concurrent_executions) { }

      public AsyncCommand(Func<T, Task> execute, Func<T, bool> can_execute, bool allow_concurrent_executions = false) {
         _execute = execute;
         _can_execute = can_execute;
         _allow_concurrent_executions = allow_concurrent_executions;
      }

      public event EventHandler CanExecuteChanged;
      public event PropertyChangedEventHandler PropertyChanged;

      public bool allow_concurrent_executions {
         get => _allow_concurrent_executions;
         set {
            _allow_concurrent_executions = value;
            if (_n_executing > 0)
               RaiseCanExecuteChanged();
         }
      }

      public bool is_executing => _n_executing > 0;

      public bool CanExecute(object parameter) {
         if (!allow_concurrent_executions && _n_executing > 0)
            return false;
          else return _can_execute(ConvertUtils.try_convert<T>(parameter));
      }

      public async Task Execute(object parameter) {
         if (++_n_executing == 1) {
            _raise_is_executing_changed();
            if (!allow_concurrent_executions)
               RaiseCanExecuteChanged();
         }
         await _execute(ConvertUtils.try_convert<T>(parameter));
         if (--_n_executing == 0) {
            _raise_is_executing_changed();
            if (!allow_concurrent_executions)
               RaiseCanExecuteChanged();
         }
      }

      void _raise_is_executing_changed() =>
         PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(is_executing)));

      void ICommand.Execute(object parameter) =>
         Execute(parameter).ignore();

      public void RaiseCanExecuteChanged() =>
         CanExecuteChanged?.Invoke(this, new EventArgs());
   }

   class AsyncCommand : AsyncCommand<object> {
      public AsyncCommand(Func<Task> execute)
         : base(_ => execute()) { }

      public AsyncCommand(Func<Task> execute, Func<bool> can_execute)
         : base(_ => execute(), _ => can_execute()) { }

      public AsyncCommand(Func<object, Task> execute)
         : base(execute) { }

      public AsyncCommand(Func<object, Task> execute, Func<object, bool> can_execute, bool allow_concurrent_executions = false)
         : base(execute, can_execute, allow_concurrent_executions) { }
   }
}