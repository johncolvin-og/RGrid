using RGrid.Utility;
using System;
using System.Linq;
using System.Windows;
using System.Windows.Markup;

namespace RGrid.WPF {
   interface IAutoFocusTargetProvider {
      FrameworkElement Target { get; }
      bool Focus { get; }
      bool FocusKeyboard { get; }
      bool ReleaseKeyboard { get; }
      bool CaptureMouse { get; }
      bool ReleaseMouse { get; }
   }

   interface IAutoFocusTargetProvider<T> : IAutoFocusTargetProvider where T : FrameworkElement {
      new T Target { get; }
   }

   static class AutoFocusTargetProvider {
      public static IAutoFocusTargetProvider<T> TypeProvider<T>(FrameworkElement root, bool focus, bool focus_keyboard, bool release_keyboard, bool capture_mouse, bool release_mouse) where T : FrameworkElement =>
         new AnonymousProvider<T>(() => root.descendants_of_type<T>().FirstOrDefault(), focus, focus_keyboard, release_keyboard, capture_mouse, release_mouse);

      public static IAutoFocusTargetProvider TypeProvider(Type type, FrameworkElement root, bool focus, bool focus_keyboard, bool release_keybobard, bool capture_mouse, bool release_mouse) =>
         new AnonymousProvider<FrameworkElement>(
            () => root.visual_children_where(el => type.IsAssignableFrom(el.GetType())).Cast<FrameworkElement>().FirstOrDefault(),
            focus, focus_keyboard, release_keybobard, capture_mouse, release_mouse);

      private class AnonymousProvider<T> : IAutoFocusTargetProvider<T> where T : FrameworkElement {
         readonly Func<T> _get_target;
         T _target;

         public AnonymousProvider(Func<T> get_target, bool focus, bool focus_keyboard, bool release_keyboard, bool capture_mouse, bool release_mouse) {
            _target = (_get_target = get_target).Invoke();
            Focus = focus;
            FocusKeyboard = focus_keyboard;
            ReleaseKeyboard = release_keyboard;
            CaptureMouse = capture_mouse;
            ReleaseMouse = release_mouse;
         }

         public T Target => _target ?? (_target = _get_target());
         public bool Focus { get; }
         public bool FocusKeyboard { get; }
         public bool ReleaseKeyboard { get; }
         public bool CaptureMouse { get; }
         public bool ReleaseMouse { get; }
         FrameworkElement IAutoFocusTargetProvider.Target => Target;

         public void refresh() =>
            _target = null;
      }
   }
   
   [MarkupExtensionReturnType(typeof(IAutoFocusTargetProvider))]
   class AutoFocusTargetExtension : MarkupExtension {
      public AutoFocusTargetExtension() { }
      public AutoFocusTargetExtension(Type Type) =>
         this.Type = Type;

      [ConstructorArgument(nameof(Type))]
      public Type Type { get; set; }
      public bool Focus { get; set; }
      public bool FocusKeyboard { get; set; }
      public bool ReleaseKeyboard { get; set; }
      public bool CaptureMouse { get; set; }
      public bool ReleaseMouse { get; set; }

      int recursion_count = 0;
      public override object ProvideValue(IServiceProvider serviceProvider) {
         ExceptionAssert.Argument.NotNull(Type, nameof(Type));
         var el = serviceProvider.GetTargetObj<FrameworkElement>();
         if (el == null) {
            ExceptionAssert.InvalidOperation.That(recursion_count++ < 5, $"Recursive tree exceeded {recursion_count - 1} calls.");
            return this;
         }
         return AutoFocusTargetProvider.TypeProvider(Type, el, Focus, FocusKeyboard, ReleaseKeyboard, CaptureMouse, ReleaseMouse);
      }
   }
}