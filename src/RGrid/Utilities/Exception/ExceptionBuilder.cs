using System;

namespace RGrid.Utility {
   class ExceptionBuilder {
      public static class Argument {
         public static ArgumentException must_be(string condition_description, string name) =>
            new ArgumentException(name);
      }
   }
}