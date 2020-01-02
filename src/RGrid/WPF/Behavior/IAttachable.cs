namespace RGrid.WPF {
   interface IAttachable<T> {
      void attach(T target);
      void detach(T target);
   }
}