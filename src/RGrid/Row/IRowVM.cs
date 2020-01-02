using System;

namespace RGrid {
   public interface IRowVM {
      event Action invalidated;
      void raise_invalidated();
   }
}