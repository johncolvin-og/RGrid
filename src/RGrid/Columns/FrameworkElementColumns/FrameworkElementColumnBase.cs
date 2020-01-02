using System.Windows;

namespace RGrid {
   public partial class DataGrid {
      public interface IFrameworkElementColumn {
         FrameworkElement view_factory();
      }

      /// <summary>
      /// Represents a column that serves as a cell-view factory for the row.
      /// <para/> Note: designed to be used by CanvasRow&lt;<typeparamref name="TColKey"/>&gt;.
      /// </summary>
      public abstract class FrameworkElementColumnBase<TColKey> : ColumnBase<TColKey>, IFrameworkElementColumn {
         protected FrameworkElementColumnBase(TColKey key) : base(key) { }

         public abstract FrameworkElement view_factory();
      }
   }
}