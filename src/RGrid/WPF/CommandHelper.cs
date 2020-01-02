using System.Windows;
using System.Windows.Input;

namespace RGrid.WPF {
   static class CommandHelper {
      #region Paraphrased from https://referencesource.microsoft.com/#PresentationFramework/src/Framework/MS/Internal/Commands/CommandHelpers.cs

      public static bool CanExecuteCommandSource(ICommandSource commandSource) {
         ICommand command = commandSource.Command;
         if (command != null) {
            object parameter = commandSource.CommandParameter;
            IInputElement target = commandSource.CommandTarget;
            RoutedCommand routed = command as RoutedCommand;
            if (routed != null) {
               if (target == null) target = commandSource as IInputElement;
               return routed.CanExecute(parameter, target);
            } else return command.CanExecute(parameter);
         }
         return false;
      }

      public static void ExecuteCommandSource(ICommandSource commandSource) {
         ICommand command = commandSource.Command;
         if (command != null) {
            object parameter = commandSource.CommandParameter;
            IInputElement target = commandSource.CommandTarget;
            RoutedCommand routed = command as RoutedCommand;
            if (routed != null) {
               if (target == null)
                  target = commandSource as IInputElement;
               if (routed.CanExecute(parameter, target))
                  routed.Execute(parameter, target);
            } else if (command.CanExecute(parameter))
               command.Execute(parameter);
         }
      }

      #endregion
   }
}