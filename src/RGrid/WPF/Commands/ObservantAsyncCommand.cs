using RGrid.Utility;
using System;
using System.ComponentModel;
using System.Threading.Tasks;
using System.Windows.Input;

namespace RGrid.WPF {
   class ObservantAsyncCommand<T> : IAsyncCommand, IObserver<bool>, INotifyPropertyChanged, IRaiseCanExecuteChangedCommand {
      bool _can_execute;
      readonly AsyncCommand<T> _command;

      public ObservantAsyncCommand(Func<T, Task> execute, bool allow_concurrent_executions = false) =>
         _command = new AsyncCommand<T>(execute, _ => _can_execute, allow_concurrent_executions);

      public event EventHandler CanExecuteChanged {
         add => _command.CanExecuteChanged += value;
         remove => _command.CanExecuteChanged -= value;
      }

      public event PropertyChangedEventHandler PropertyChanged {
         add => _command.PropertyChanged += value;
         remove => _command.PropertyChanged -= value;
      }

      public bool allow_concurrent_executions {
         get => _command.allow_concurrent_executions;
         set => _command.allow_concurrent_executions = value;
      }

      public bool is_executing => _command.is_executing;

      public bool CanExecute(object parameter) => _command.CanExecute(parameter);
      public async Task Execute(object parameter) => await _command.Execute(parameter);

      public void OnCompleted() { }
      public void OnError(Exception error) { }
      public void OnNext(bool value) {
         _can_execute = value;
         _command.RaiseCanExecuteChanged();
      }

      void ICommand.Execute(object parameter) =>
         _command.Execute(parameter).ignore();

      void IRaiseCanExecuteChangedCommand.RaiseCanExecuteChanged() =>
         _command.RaiseCanExecuteChanged();
   }

   class ObservantAsyncCommand : ObservantAsyncCommand<object> {
      public ObservantAsyncCommand(Func<Task> execute, bool allow_concurrent_executions = false)
         : base(_ => execute(), allow_concurrent_executions) { }

      public ObservantAsyncCommand(Func<object, Task> execute, bool allow_concurrent_executions = false)
         : base(execute, allow_concurrent_executions) { }
   }
}
