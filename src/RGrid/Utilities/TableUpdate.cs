using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

namespace RGrid.Utility {
   public readonly struct Change<T> {
      public Change(T o, T n) {
         Old = o;
         New = n;
      }
      public readonly T Old;
      public readonly T New;
      public Change<T> merge(Change<T> newer) {
         return new Change<T>(Old, newer.New);
      }
      public void Deconstruct(out T Old, out T New) {
         Old = this.Old;
         New = this.New;
      }
      public override string ToString() {
         return $"{{{Old}=>{New}}}";
      }
   }

   public class TableUpdate<T, TKey> where TKey : IEquatable<TKey> {
      // warning: this does not have a key_extractor
      public static readonly TableUpdate<T, TKey> empty = new TableUpdate<T, TKey>(ImmutableDictionary<TKey, T>.Empty, Array.Empty<T>(), Array.Empty<Change<T>>(), Array.Empty<T>(), null);

      readonly ImmutableDictionary<TKey, T> _state;

      public static TableUpdate<T, TKey> mk_empty(Func<T, TKey> key_extractor) => new TableUpdate<T, TKey>(ImmutableDictionary<TKey, T>.Empty, Array.Empty<T>(), Array.Empty<Change<T>>(), Array.Empty<T>(), key_extractor);

      internal TableUpdate(
         ImmutableDictionary<TKey, T> init,
         IEnumerable<T> added,
         IEnumerable<Change<T>> changed,
         IEnumerable<T> removed,
         Func<T, TKey> maybe_key_extractor) {
         _state = init;
         this.added = added;
         this.changes = changed;
         this.removed = removed;
         this.key_extractor = maybe_key_extractor;
      }

      public IReadOnlyDictionary<TKey, T> state { get { return _state; } }
      public IEnumerable<T> added { get; }
      public IEnumerable<Change<T>> changes { get; }
      public IEnumerable<T> changed => changes.Select(c => c.New);
      public IEnumerable<T> removed { get; }

      internal Func<T, TKey> key_extractor { get; private set; }

      internal static TableUpdate<T, TKey> update(TableUpdate<T, TKey> table, IEnumerable<T> added, IEnumerable<Change<T>> changes, IEnumerable<T> removed, Func<T, TKey> key) {
         var builder = table._state.ToBuilder();
         builder.RemoveRange(removed.Select(i => key(i)));
         builder.AddRange(added.Select(i => new KeyValuePair<TKey, T>(key(i), i)));
         foreach (var i in changes) {
            builder[key(i.New)] = i.New;
         }
         return new TableUpdate<T, TKey>(builder.ToImmutableDictionary(), added, changes, removed, key);
      }

      internal static TableUpdate<T, TKey> update(TableUpdate<T, TKey> table, IReadOnlyDictionary<TKey, T> new_state, IEqualityComparer<T> value_comparer) {
         var builder = table._state.ToBuilder();
         // removed/changes
         var removed = new List<T>();
         var changes = new List<Change<T>>();
         foreach (var kv in table._state) {
            if (new_state.TryGetValue(kv.Key, out T new_val)) {
               if (!value_comparer.Equals(kv.Value, new_val)) {
                  changes.Add(new Change<T>(kv.Value, new_val));
                  builder[kv.Key] = new_val;
               }
            } else {
               removed.Add(kv.Value);
               builder.Remove(kv.Key);
            }
         }
         // added
         var added = new List<T>();
         foreach (var kv in new_state) {
            if (!table._state.ContainsKey(kv.Key)) {
               added.Add(kv.Value);
               builder.Add(kv);
            }
         }
         return new TableUpdate<T, TKey>(builder.ToImmutableDictionary(), added, changes, removed, table.key_extractor);
      }

      internal TableUpdate<T, TKey> as_snapshot() {
         return new TableUpdate<T, TKey>(_state, _state.Values.ToList(), new Change<T>[0], new T[0], key_extractor);
      }

      internal Builder to_builder() =>
         new Builder(this);

      public static class FilterHelper {
         internal static TableUpdate<T, TKey> on_source_changed(TableUpdate<T, TKey> filtered_target, IEnumerable<T> raw_added, IEnumerable<Change<T>> raw_changes, IEnumerable<T> raw_removed, Func<T, bool> predicate) {
            var builder = filtered_target._state.ToBuilder();
            List<T> added, removed;
            List<Change<T>> changes;
            if (predicate == null) {
               added = raw_added.ToList();
               removed = raw_removed.ToList();
               changes = raw_changes.ToList();
            } else {
               added = new List<T>(raw_added.Where(predicate));
               removed = new List<T>(raw_removed.Where(predicate));
               changes = new List<Change<T>>();
               foreach (var ch in raw_changes) {
                  if (predicate(ch.Old)) {
                     if (predicate(ch.New)) {
                        changes.Add(ch);
                     } else {
                        removed.Add(ch.Old);
                     }
                  } else if (predicate(ch.New)) {
                     added.Add(ch.New);
                  }
               }
            }
            var key = filtered_target.key_extractor;
            builder.RemoveRange(removed.Select(key));
            foreach (var ch in changes)
               builder[key(ch.New)] = ch.New;
            builder.AddRange(added.Select(v => new KeyValuePair<TKey, T>(key(v), v)));
            return new TableUpdate<T, TKey>(builder.ToImmutable(), added, changes, removed, key);
         }

         internal static TableUpdate<T, TKey> on_predicate_changed(TableUpdate<T, TKey> filtered_target, IReadOnlyDictionary<TKey, T> unfiltered_state, Func<T, bool> old_predicate, Func<T, bool> new_predicate) {
            var added = new List<T>();
            var removed = new List<T>();
            if (new_predicate == old_predicate) {
               // nothing changed - this method probably shouldn't be called when nothing changes.  Having said that, it doesn't really hurt anything,
               // so don't throw a metaphorical wrench into the gears by throwing a literal exception. Just return the table as is.
               if (filtered_target.has_changes())
                  filtered_target = new TableUpdate<T, TKey>(filtered_target._state, Enumerable.Empty<T>(), Enumerable.Empty<Change<T>>(), Enumerable.Empty<T>(), filtered_target.key_extractor);
               return filtered_target;
            }
            if (old_predicate == null) {
               removed.AddRange(filtered_target.state.Values.Where(v => !new_predicate(v)));
            } else if (new_predicate == null) {
               added.AddRange(unfiltered_state.Values.Where(v => !old_predicate(v)));
            } else {
               foreach (var kv in unfiltered_state) {
                  if (old_predicate(kv.Value)) {
                     if (new_predicate(kv.Value)) {
                        continue;
                     } else {
                        removed.Add(kv.Value);
                     }
                  } else if (new_predicate(kv.Value)) {
                     added.Add(kv.Value);
                  }
               }
            }
            var builder = filtered_target._state.ToBuilder();
            var key = filtered_target.key_extractor;
            builder.RemoveRange(removed.Select(key));
            builder.AddRange(added.Select(v => new KeyValuePair<TKey, T>(key(v), v)));
            return new TableUpdate<T, TKey>(builder.ToImmutable(), added, Array.Empty<Change<T>>(), removed, key);
         }
      }

      /// <summary>
      /// This class is used to aggregate a series of changes into a single update.
      /// </summary>
      internal class Builder {
         readonly TableUpdate<T, TKey> _state;
         readonly Dictionary<TKey, T> _added = new Dictionary<TKey, T>(), _removed = new Dictionary<TKey, T>();
         readonly Dictionary<TKey, Change<T>> _changes = new Dictionary<TKey, Change<T>>();

         public Builder(TableUpdate<T, TKey> state) =>
            _state = state;

         public void add_or_update(T value) =>
            add_or_update(_state.key_extractor(value), value);

         public void add_or_update(TKey key, T value) {
            if (_state.state.TryGetValue(key, out T curr)) {
               _removed.Remove(key);
               _changes[key] = new Change<T>(curr, value);
            } else {
               _added[key] = value;
            }
         }

         public void remove(TKey key) {
            if (_state.state.TryGetValue(key, out T orig)) {
               _removed[key] = orig;
               _changes.Remove(key);
            } else {
               _added.Remove(key);
            }
         }

         public void merge_table(TableUpdate<T, TKey> table, Func<T, TKey> _key) {
            foreach (T v in table.removed)
               remove(_key(v));
            foreach (T v in table.added.Concat(table.changed))
               add_or_update(_key(v), v);
         }

         public void clear() {
            _added.Clear();
            _changes.Clear();
            foreach (var kv in _state.state)
               _removed[kv.Key] = kv.Value;
         }

         public TableUpdate<T, TKey> to_table() {
            var builder = _state._state.ToBuilder();
            builder.RemoveRange(_removed.Keys);
            foreach (var kv in _changes)
               builder[kv.Key] = kv.Value.New;
            builder.AddRange(_added);
            return new TableUpdate<T, TKey>(builder.ToImmutableDictionary(), _added.Values, _changes.Values, _removed.Values, _state.key_extractor);
         }

         public TableUpdate<T, TKey> to_table_checked(IEqualityComparer<T> comparer) {
            var builder = _state._state.ToBuilder();
            builder.RemoveRange(_removed.Keys);
            foreach (var kv in _changes) {
               if (!comparer.Equals(kv.Value.Old, kv.Value.New))
                  builder[kv.Key] = kv.Value.New;
            }
            builder.AddRange(_added);
            return new TableUpdate<T, TKey>(builder.ToImmutableDictionary(), _added.Values, _changes.Values, _removed.Values, _state.key_extractor);
         }
      }
   }

   public static class ObservableUpdateExtensions {
      public static IEnumerable<T> Old<T>(this IEnumerable<Change<T>> changes) {
         foreach (var ch in changes)
            yield return ch.Old;
      }

      public static IEnumerable<T> New<T>(this IEnumerable<Change<T>> changes) {
         foreach (var ch in changes)
            yield return ch.New;
      }

      public static IEnumerable<Change<T>> Actual<T>(this IEnumerable<Change<T>> changes) where T : IEquatable<T> {
         foreach (var ch in changes) {
            if (!ch.New.Equals(ch.Old))
               yield return ch;
         }
      }

      public static IEnumerable<Change<T>> Actual<T>(this IEnumerable<Change<T>> changes, IEqualityComparer<T> comparer) {
         foreach (var ch in changes) {
            if (!comparer.Equals(ch.New, ch.Old))
               yield return ch;
         }
      }

      internal static bool has_changes<T, TKey>(this TableUpdate<T, TKey> table) where TKey : IEquatable<TKey> =>
         table.added.Any() || table.changes.Any() || table.removed.Any();

      public static TableUpdate<T, TKey> with_added<T, TKey>(this TableUpdate<T, TKey> table, IEnumerable<T> added) where TKey : IEquatable<TKey> =>
         TableUpdate<T, TKey>.update(table, added, Enumerable.Empty<Change<T>>(), Enumerable.Empty<T>(), table.key_extractor);

      public static TableUpdate<T, TKey> with_removed<T, TKey>(this TableUpdate<T, TKey> table, IEnumerable<T> removed) where TKey : IEquatable<TKey> =>
         TableUpdate<T, TKey>.update(table, Enumerable.Empty<T>(), Enumerable.Empty<Change<T>>(), removed, table.key_extractor);

      public static TableUpdate<T, TKey> with_changed<T, TKey>(this TableUpdate<T, TKey> table, IEnumerable<T> changed) where TKey : IEquatable<TKey> =>
         TableUpdate<T, TKey>.update(table, Enumerable.Empty<T>(), changed.Select(v => new Change<T>(table.state[table.key_extractor(v)], v)), Enumerable.Empty<T>(), table.key_extractor);

      public static TableUpdate<T, TKey> with_changes<T, TKey>(this TableUpdate<T, TKey> table, IEnumerable<Change<T>> changes) where TKey : IEquatable<TKey> =>
         TableUpdate<T, TKey>.update(table, Enumerable.Empty<T>(), changes, Enumerable.Empty<T>(), table.key_extractor);
   }
}
