using System;
using System.Windows.Input;

namespace RGrid.WPF {
   class ObservantCommand<T> : ICommand, IObserver<bool>, IRaiseCanExecuteChangedCommand {
      readonly Action<T> _execute;
      bool _can_execute;

      public ObservantCommand(Action<T> execute) =>
         _execute = execute;

      public event EventHandler CanExecuteChanged;

      public bool CanExecute(object parameter) => _can_execute;
      public void Execute(object parameter) => _execute(ConvertUtils.try_convert<T>(parameter));

      public void OnCompleted() { }
      public void OnError(Exception error) { }
      public void OnNext(bool value) {
         _can_execute = value;
         _raise_can_execute_changed();
      }

      void _raise_can_execute_changed() =>
         CanExecuteChanged?.Invoke(this, new EventArgs());

      void IRaiseCanExecuteChangedCommand.RaiseCanExecuteChanged() =>
         _raise_can_execute_changed();
   }

   class ObservantCommand : ObservantCommand<object> {
      public ObservantCommand(Action execute) : base(_ => execute()) { }
      public ObservantCommand(Action<object> execute) : base(execute) { }
   }
}