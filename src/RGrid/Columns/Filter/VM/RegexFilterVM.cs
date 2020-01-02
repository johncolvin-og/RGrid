//using System;
//using System.Text.RegularExpressions;

//namespace RGrid.Filters {
//   public class RegexFilterVM<TRow> : FilterVMBase<TRow, string, string> {
//      string _text = string.Empty;
//      Regex _regex;

//      public RegexFilterVM(Func<TRow, string> get_row_val, string prop_name) : base(get_row_val, prop_name) { }

//      public string text {
//         get => _text;
//         set {
//            if (value != null) {
//               _regex = (_text = value.Trim()) == string.Empty ?
//                  null : new Regex($"{_text = value}", RegexOptions.IgnorePatternWhitespace | RegexOptions.IgnoreCase);
//            } else {
//               _text = string.Empty;
//               _regex = null;
//            }
//         }
//      }

//      public override string get_state() => _text ?? string.Empty;

//      protected override bool _get_active() => _regex != null;
//      protected override bool _filter(string value) => _regex != null && _regex.IsMatch(value);
//      protected override void _load_state_internal(string state) => text = state;
//      protected override void _clear() {
//         _close();
//         _text = string.Empty;
//         _regex = null;
//         RaisePropertyChanged(nameof(text));
//         _raise_filter_changed();
//      }
//   }
//}




using System;
using System.Text.RegularExpressions;

namespace RGrid.Filters {
   static class RegexFilterUtils {
      public static Regex build_regex(string str) {
         if (str == null)
            return null;
         str = str.Trim();
         if (str == string.Empty)
            return null;
         return new Regex(str, RegexOptions.IgnoreCase | RegexOptions.IgnorePatternWhitespace);
      }
   }
   public class RegexFilterVM<TRow> : FilterVMBase<TRow, string, string> {
      string _text = string.Empty;
      Regex _regex;

      public RegexFilterVM(Func<TRow, string> get_row_val, string prop_name = null) : base(get_row_val, prop_name) { }

      public string text {
         get => _text;
         set => _regex = RegexFilterUtils.build_regex(_text = (value ?? string.Empty));
      }

      public override string GetState() => _text ?? string.Empty;

      protected override bool _get_active() => _regex != null;
      protected override bool _filter(string value) => _regex != null && _regex.IsMatch(value);
      protected override void _load_state_internal(string state) => text = state;
      protected override void _clear() {
         _close();
         _text = string.Empty;
         _regex = null;
         RaisePropertyChanged(nameof(text));
         _raise_filter_changed();
      }
   }
}