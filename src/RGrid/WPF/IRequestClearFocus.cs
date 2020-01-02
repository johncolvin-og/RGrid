using Disposable.Extensions;
using System;
using System.ComponentModel;

namespace RGrid.WPF {
   interface IRequestClearFocus {
      event Action<(bool clear_focus, bool clear_keyboard_focus)> request_clear_focus;
   }

   [Browsable(false)]
   [EditorBrowsable(EditorBrowsableState.Never)]
   static class RequestClearFocusExtensions {
      public static IDisposable subscribe(this IRequestClearFocus source, Action<(bool clear_focus, bool clear_keyboard_focus)> callback) {
         source.request_clear_focus += callback;
         return DisposableFactory.Create(() => source.request_clear_focus -= callback);
      }
   }
}