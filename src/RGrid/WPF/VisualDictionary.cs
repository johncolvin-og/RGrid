using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Media;

namespace RGrid.WPF {
   /// <summary>
   /// Represents a collection of keys and [Visual]values.  All values are added to the parent's VisualTree.
   /// <para/>Note: All visuals added to the parent through VisualDictionary may be accesed by index through the VisualAtIndex method.
   /// </summary>
   class VisualDictionary<TKey, TValue> : IDictionary<TKey, TValue> where TValue : Visual {
      readonly VisualCollection _collection;
      readonly Dictionary<TKey, TValue> _dictionary = new Dictionary<TKey, TValue>();

      public VisualDictionary(Visual parent) =>
         _collection = new VisualCollection(parent);

      public TValue this[TKey key] {
         get => _dictionary[key];
         set {
            if (_dictionary.TryGetValue(key, out TValue v))
               _collection.Remove(v);
            _dictionary[key] = v;
            _collection.Add(value);
         }
      }
      public ICollection<TKey> Keys => _dictionary.Keys;
      public ICollection<TValue> Values => _dictionary.Values;
      public int Count => _dictionary.Count;
      public bool IsReadOnly => false;
      public void Add(TKey key, TValue value) {
         _dictionary.Add(key, value);
         _collection.Add(value);
      }
      public void Add(KeyValuePair<TKey, TValue> item) => Add(item.Key, item.Value);
      public void Clear() {
         _dictionary.Clear();
         _collection.Clear();
      }
      public bool Contains(KeyValuePair<TKey, TValue> item) => _dictionary.Contains(item);
      public bool ContainsKey(TKey key) => _dictionary.ContainsKey(key);
      public void CopyTo(KeyValuePair<TKey, TValue>[] array, int arrayIndex) {
         ((ICollection<KeyValuePair<TKey, TValue>>)_dictionary).CopyTo(array, arrayIndex);
      }
      public IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator() => _dictionary.GetEnumerator();
      public bool Remove(TKey key) {
         if (_dictionary.TryGetValue(key, out TValue value))
            _collection.Remove(value);
         return _dictionary.Remove(key);
      }
      public bool Remove(KeyValuePair<TKey, TValue> item) {
         if (((ICollection<KeyValuePair<TKey, TValue>>)_dictionary).Remove(item)) {
            _collection.Remove(item.Value);
            return true;
         }
         return false;
      }
      public bool TryGetValue(TKey key, out TValue value) => _dictionary.TryGetValue(key, out value);
      public Visual VisualAtIndex(int index) => _collection[index];
      IEnumerator IEnumerable.GetEnumerator() => _dictionary.GetEnumerator();
   }
}