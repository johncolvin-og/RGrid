using System.Windows.Controls;

namespace RGrid.Utility {
   interface IHandleRowSelection {
      void row_selection_changed(SelectionChangedEventArgs e);
   }
}