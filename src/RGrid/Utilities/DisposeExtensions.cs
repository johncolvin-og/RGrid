using Disposable.Extensions;
using Disposable.Extensions.Utilities;
using System;
using System.Collections;
using System.Windows;

namespace RGrid.Utility {
   public interface IObjectDisposer : IDisposable
   {
      event Action RequestDispose;
   }

   public interface IWillLetYouKnowWhenIDispose {
      event Action<bool> OnDisposed;
   }

   public class ObjectDisposer : IObjectDisposer
	{
		public event Action RequestDispose;
		public void Dispose() {
			if(RequestDispose != null)
				RequestDispose();	
		}
	}
	public static class DisposeExtensions
	{
		public static readonly DependencyProperty DisposerProperty = DependencyProperty.RegisterAttached("Disposer", typeof(IObjectDisposer), typeof(DisposeExtensions),
			new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.Inherits, (d, e) => {
				if (e.OldValue is IObjectDisposer && e.NewValue is IObjectDisposer)
					((IObjectDisposer)e.OldValue).Dispose();
			}));

		public static IObjectDisposer GetDisposer(DependencyObject d) { return d.GetValue(DisposerProperty) as IObjectDisposer; }
		public static void SetDisposer(DependencyObject d, IObjectDisposer value) { d.SetValue(DisposerProperty, value); }

		public static readonly DependencyProperty DisposablesProperty = DependencyProperty.RegisterAttached("Disposables", typeof(IList), typeof(DisposeExtensions));
		public static IList GetDisposables(DependencyObject d) { return d.GetValue(DisposablesProperty) as IList; }
		public static void SetDisposables(DependencyObject d, IList value) { d.SetValue(DisposablesProperty, value); }

		private static bool _try_attach_to_disposer(DependencyObject target, IDisposable dispose) {
			IObjectDisposer disposer = GetDisposer(target);
			if (disposer == null)
				return false;
			Action destroy = null;
			destroy = () => {
				DisposableUtils.Dispose(ref dispose);
				disposer.RequestDispose -= destroy;
				destroy = null;
			};
			disposer.RequestDispose += destroy;
			return true;
		}

      public static IDisposable SubscribeDisposed(this IWillLetYouKnowWhenIDispose dispose_broadcaster, Action callback) =>
         dispose_broadcaster.SubscribeDisposed(_ => { callback?.Invoke(); callback = null; });

      public static IDisposable SubscribeDisposed(this IWillLetYouKnowWhenIDispose dispose_broadcaster, IDisposable dispose) =>
         dispose_broadcaster.SubscribeDisposed(_ => DisposableUtils.Dispose(ref dispose));

      public static IDisposable SubscribeDisposed(this IWillLetYouKnowWhenIDispose dispose_broadcaster, Action<bool> callback) {
         dispose_broadcaster.OnDisposed += callback;
         return DisposableFactory.Create(() => dispose_broadcaster.OnDisposed -= callback);
      }
	}
}
