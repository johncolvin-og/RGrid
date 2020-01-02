using Disposable.Extensions;
using Disposable.Extensions.Utilities;
using System;
using System.Reactive.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using CompositeDisposable = System.Reactive.Disposables.CompositeDisposable;

namespace RGrid.Utility {
   static class FrameworkElementHelper {
      public static Point GetCenter(this FrameworkElement element) =>
         new Point(element.ActualWidth * 0.5, element.ActualHeight * 0.5);

      public static Point GetChildCenter(this UIElement parent, FrameworkElement child) =>
         child.TranslatePoint(GetCenter(child), parent);

      public static Point GetChildTopLeft(this UIElement parent, FrameworkElement child) =>
         child.TranslatePoint(new Point(0, 0), parent);

      public static Rect GetChildBounds(this UIElement target, FrameworkElement child) {
         var child_tl = target.GetChildTopLeft(child);
         return new Rect(child_tl.X, child_tl.Y, child.ActualWidth, child.ActualHeight);
      }

      /// <summary>
      /// Invokes the specified action immediately if the element is already loaded, otherwise hooks up to the <see cref="FrameworkElement.Loaded"/> event,
      /// and [conditionally]invokes the specified action if <paramref name="should_still_execute"/> returns true.
      /// </summary>
      /// <param name="element">The element that must be loaded before the specified action is invoked.</param>
      /// <param name="action">The action to invoke when the element is loaded.</param>
      /// <param name="should_still_execute">If the element is not already loaded, this delegate determines whether or not to invoke the action in the Loaded event callback.</param>
      /// <returns></returns>
      public static void InvokeOnLoaded(this FrameworkElement element, Action action, Func<bool> should_still_execute) {
         if (action == null)
            throw new ArgumentNullException(nameof(action));
         if (element.IsLoaded) {
            action();
         } else {
            element.Loaded += on_loaded;
            void on_loaded(object sender, RoutedEventArgs e) {
               element.Loaded -= on_loaded;
               if (should_still_execute())
                  action();
            }
         }
      }

      public static IDisposable SubscribeLoaded(this FrameworkElement element, RoutedEventHandler callback) {
         element.Loaded += callback;
         return DisposableFactory.Create(() => element.Loaded -= callback);
      }

      public static IDisposable SubscribeSizeChanged(this FrameworkElement element, Action callback) =>
         SubscribeSizeChanged(element, (s, e) => callback());

      public static IDisposable SubscribeSizeChanged(this FrameworkElement element, SizeChangedEventHandler callback) {
         element.SizeChanged += callback;
         return DisposableFactory.Create(() => element.SizeChanged -= callback);
      }

      public static IDisposable SubscribeDataContextDisposed(this FrameworkElement element, Func<IDisposable> set_hooks) {
         IDisposable data_context_sub = null;
         subscribe_data_context(element.DataContext as IWillLetYouKnowWhenIDispose);
         element.DataContextChanged += on_data_context_changed;
         return DisposableFactory.Create(() => {
            element.DataContextChanged -= on_data_context_changed;
            DisposableUtils.Dispose(ref data_context_sub);
         });
         void on_data_context_changed(object sender, DependencyPropertyChangedEventArgs e) =>
            subscribe_data_context(e.NewValue as IWillLetYouKnowWhenIDispose);

         void on_data_context_disposed(bool disposing) =>
            DisposableUtils.Dispose(ref data_context_sub);

         void subscribe_data_context(IWillLetYouKnowWhenIDispose dc) {
            DisposableUtils.Dispose(ref data_context_sub);
            if (dc != null) {
               dc.OnDisposed += on_data_context_disposed;
               data_context_sub = DisposableFactory.Create(set_hooks().Dispose, () => dc.OnDisposed -= on_data_context_disposed);
            }
         }
      }

      public static IDisposable ConnectDataContext<T>(this FrameworkElement element, Action<T> connect, Action<T> disconnect) =>
         ConnectDataContext<T>(element, vm => {
            connect(vm);
            return DisposableFactory.Create(() => disconnect(vm));
         });

      public static IDisposable ConnectDataContext<T>(this FrameworkElement element, Func<T, IDisposable> connect) {
         var connection = element.DataContext is T vm ? connect(vm) : null;
         element.DataContextChanged += on_data_context_changed;
         return DisposableFactory.Create(dispose);
         //
         void on_data_context_changed(object sender, DependencyPropertyChangedEventArgs e) {
            connection?.Dispose();
            connection = e.NewValue is T new_vm ? connect(new_vm) : null;
         }

         void dispose() {
            DisposableUtils.Dispose(ref connection);
            element.DataContextChanged -= on_data_context_changed;
         }
      }

      public static IObservable<bool> ObserveIsChecked(this ToggleButton toggle_button) =>
         Observable.Create<bool>(o => {
            toggle_button.Checked += on_checked;
            toggle_button.Unchecked += on_unchecked;
            return DisposableFactory.Create(dispose);
            void on_checked(object sender, RoutedEventArgs e) => o.OnNext(true);
            void on_unchecked(object sender, RoutedEventArgs e) => o.OnNext(false);
            void dispose() {
               toggle_button.Checked -= on_checked;
               toggle_button.Unchecked -= on_unchecked;
            }
         });

      public static IObservable<bool> ObserveIsChecked(this MenuItem menu_item) =>
         Observable.Create<bool>(o => {
            menu_item.Checked += on_checked;
            menu_item.Unchecked += on_unchecked;
            return DisposableFactory.Create(dispose);
            void on_checked(object sender, RoutedEventArgs e) => o.OnNext(true);
            void on_unchecked(object sender, RoutedEventArgs e) => o.OnNext(false);
            void dispose() {
               menu_item.Checked -= on_checked;
               menu_item.Unchecked -= on_unchecked;
            }
         });
      public static Task<RoutedEventArgs> LoadedAsync(this FrameworkElement element) {
         if (element.IsLoaded)
            return Task.FromResult(new RoutedEventArgs(FrameworkElement.LoadedEvent, element));
         var tcs = new TaskCompletionSource<RoutedEventArgs>();
         element.Loaded += on_loaded;
         return tcs.Task;
         void on_loaded(object sender, RoutedEventArgs e) {
            element.Loaded -= on_loaded;
            tcs.TrySetResult(e);
         }
      }

      public static IDisposable ConnectOnLoaded(this FrameworkElement element, Func<IDisposable> connect, bool disconnect_on_unloaded = true) {
         IDisposable connection = null;
         element.Loaded += on_loaded;
         if (disconnect_on_unloaded)
            element.Unloaded += on_unloaded;
         if (element.IsLoaded)
            connection = connect();
         return DisposableFactory.Create(dispose);

         void on_loaded(object sender, RoutedEventArgs e) {
            connection?.Dispose();
            connection = connect();
         }

         void on_unloaded(object sender, RoutedEventArgs e) =>
            DisposableUtils.Dispose(ref connection);

         void dispose() {
            DisposableUtils.Dispose(ref connection);
            element.Loaded -= on_loaded;
            if (disconnect_on_unloaded)
               element.Unloaded -= on_unloaded;
         }
      }
   }
}
