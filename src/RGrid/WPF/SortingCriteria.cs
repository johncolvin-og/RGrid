using ProtoBuf;
using System;
using System.ComponentModel;

namespace RGrid.Utility {
   [Serializable]
   [ProtoContract]
   public class SortingCriteria {
      public SortingCriteria() { }
      public SortingCriteria(SortDescription sort_description) : this(sort_description.PropertyName, sort_description.Direction == ListSortDirection.Ascending) { }
      public SortingCriteria(string col_name, bool is_ascending) {
         this.col_name = col_name;
         this.is_ascending = is_ascending;
      }

      [ProtoMember(1)]
      public string col_name { get; set; }
      [ProtoMember(2)]
      public bool is_ascending { get; set; }

      public SortDescription to_sort_description() { return new SortDescription(col_name, is_ascending ? ListSortDirection.Ascending : ListSortDirection.Descending); }
   }

   static class SortingCriteriaExtensions {
      public static ListSortDirection direction(this SortingCriteria sorting_criteria) =>
         sorting_criteria.is_ascending ? ListSortDirection.Ascending : ListSortDirection.Descending;
   }
}
