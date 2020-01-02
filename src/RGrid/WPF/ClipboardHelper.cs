using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Windows;

namespace RGrid.Utility {
   static class ClipboardHelper {
      public const string ExcelRowSeparator = "\r\n",
                          ExcelCellSeparator = "\t";

      public static string GetExcelFormattedText(IEnumerable<string[]> rows) =>
         string.Join(ExcelRowSeparator, rows.Select(r => string.Join(ExcelCellSeparator, r)));

      public static void CopyTextToClipboard(string text) {
         int n_tries = 0;
         while (n_tries++ < 5) {
            try {
               Clipboard.SetText(text);
               break;
            } catch {
               Thread.Sleep(1);
            }
         }
      }
   }
}