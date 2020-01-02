using Collections.Sync.Utils;
using Disposable.Extensions;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;

namespace RGrid.Utility
{
   public interface IReadOnlyObservableCollectionEx : ICollection, IEnumerable, INotifyPropertyChanged { }

   public interface IReadOnlyObservableCollectionEx<T> : IReadOnlyList<T>, IReadOnlyCollection<T>, IEnumerable<T>, IEnumerable,  INotifyCollectionChanged { }

   public interface IObservableCollectionEx : IList, ICollection, IEnumerable, INotifyCollectionChanged, IReadOnlyObservableCollectionEx
   {
      void SetItems(IEnumerable items);
   }

   public interface IObservableCollectionEx<T> : IList<T>, ICollection<T>, IList, ICollection, IReadOnlyObservableCollectionEx<T>, IReadOnlyList<T>, IReadOnlyCollection<T>, IEnumerable<T>, IEnumerable, INotifyCollectionChanged
   {
      void SetItems(IEnumerable<T> items);
      void AddRange(IEnumerable<T> items);
   }

   public interface INotifierCollection<T> : IReadOnlyList<T>, ICollection<T>, INotifyCollectionChanged, INotifyPropertyChanged { }

   public class ObservableCollectionEx<T> : ObservableCollection<T>, IObservableCollectionEx<T>, IObservableCollectionEx
   {
      public ObservableCollectionEx() { }

      public ObservableCollectionEx(IEnumerable<T> items) {
         using (quiet_change()) foreach (var item in items) Add(item);
      }

      private const string CountString = "Count";
      // This must agree with Binding.IndexerName.
      private const string IndexerName = "Item[]";
      private bool _events_suspend;

      private IDisposable quiet_change() {
         if (_events_suspend) throw new InvalidOperationException("Events are already suspended.");
         _events_suspend = true;
         return DisposableFactory.Create(() => _events_suspend = false);
      }

      private void _fire_events(NotifyCollectionChangedEventArgs e) {
         base.OnPropertyChanged(new PropertyChangedEventArgs(CountString));
         base.OnPropertyChanged(new PropertyChangedEventArgs(IndexerName));
         base.OnCollectionChanged(e);
      }

      protected override void OnCollectionChanged(NotifyCollectionChangedEventArgs e) { if (!_events_suspend) base.OnCollectionChanged(e); }

      public void SetItems(IEnumerable<T> items) {
         base.ClearItems();
         this.AddRange(items);
      }

      public void AddRange(IEnumerable<T> items) {
         int starting_index = Count;
         using (quiet_change()) foreach (var item in items) base.Add(item); 
         _fire_events(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add, items.ToList(), starting_index)); 
      }

      public void SetItems(IEnumerable items) { SetItems(items.Cast<T>()); }
   } 
}
