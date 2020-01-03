using System;
using System.ComponentModel;
using System.Windows.Input;

namespace RGrid.Filters {
   public interface IDataGridColumnFilter : INotifyDataErrorInfo {
      string prop_name { get; }
      bool? IsActive { get; }
      bool IsOpen { get; }
      ICommand Apply { get; }
      ICommand Clear { get; }
      ICommand Cancel { get; }
      bool Filter(object row);
      event Action FilterChanged;
   }

   public interface IDataGridColumnFilter<in TRow> : IDataGridColumnFilter {
      bool Filter(TRow row);
   }

   public interface IDataGridColumnPersistentFilter<in TRow, TState> : IDataGridColumnFilter<TRow> {
      void LoadState(TState state);
      TState GetState();
   }
}