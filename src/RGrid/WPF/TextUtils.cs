using RGrid.WPF;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Media;

namespace RGrid.Utility {
   static class TextUtils {
      public const string WPFNewLine = "&#x0a;";
      public static readonly string StandardFontFamilyName;
      public static readonly System.Windows.Media.FontFamily StandardFontFamily;
      public static readonly Typeface StandardTypeface;
      public static readonly GlyphTypeface StandardGlyphTypeface;
      public static readonly GlyphContext StandardGlyphContext;

      static TextUtils() {
         // try to guarentee a standard font
         var preferred_fonts = new[] {
            new System.Windows.Media.FontFamily("Segoe UI"),
            SystemFonts.MessageFontFamily,
            (System.Windows.Media.FontFamily)TextElement.FontFamilyProperty.DefaultMetadata.DefaultValue
         };
         foreach (var f in preferred_fonts) {
            StandardTypeface = new Typeface(f, FontStyles.Normal, FontWeights.Normal, FontStretches.Normal);
            if (StandardTypeface.TryGetGlyphTypeface(out StandardGlyphTypeface)) {
               StandardGlyphContext = GlyphContext.create(StandardGlyphTypeface);
               StandardFontFamily = f;
               StandardFontFamilyName = f.FamilyNames.First().Value;
               break;
            }
         }
      }

      public static FormattedText GetFormattedText(string text) { return GetFormattedText(text, FontSizes.Regular); }
      public static FormattedText GetFormattedText(string text, double size) { return GetFormattedText(text, size, SystemColors.ControlTextBrush); }
      public static FormattedText GetFormattedText(string text, double size, Brush brush) => GetFormattedText(text, size, brush, StandardTypeface);
      public static FormattedText GetFormattedText(string text, double size, Brush brush, Typeface typeface) {
#pragma warning disable CS0618 // Type or member is obsolete
         return new FormattedText(text, CultureInfo.CurrentCulture, FlowDirection.LeftToRight, typeface, size, brush);
#pragma warning restore CS0618 // Type or member is obsolete
      }

      public static FormattedText GetFormattedTextBold(string text) => GetFormattedTextBold(text, FontSizes.Regular);
      public static FormattedText GetFormattedTextBold(string text, double size) => GetFormattedTextBold(text, size, SystemColors.ControlTextBrush);
      public static FormattedText GetFormattedTextBold(string text, double size, Brush brush) => GetFormattedTextBold(text, size, brush, StandardTypeface);
      public static FormattedText GetFormattedTextBold(string text, double size, Brush brush, Typeface typeface) {
#pragma warning disable CS0618 // Type or member is obsolete
         var ft = new FormattedText(text, CultureInfo.CurrentCulture, FlowDirection.LeftToRight, typeface, size, brush);
#pragma warning restore CS0618 // Type or member is obsolete
         ft.SetFontWeight(FontWeights.Bold);
         return ft;
      }

      public static Typeface with(this Typeface typeface, FontWeight font_weight) =>
         new Typeface(typeface.FontFamily, typeface.Style, font_weight, typeface.Stretch);

      public static Typeface with(this Typeface typeface, System.Windows.Media.FontFamily font_family) =>
         new Typeface(font_family, typeface.Style, typeface.Weight, typeface.Stretch);

      /// <summary>
      /// Ensures weight_value is within the acceptable range (1-999) to avoid an ArgumentOutOfRangeException when calling FontWeight.FromOpenTypeWeight.
      /// </summary>
      /// <param name="weight_value"></param>
      /// <returns></returns>
      public static FontWeight from_open_type_weight_safe(int weight_value) =>
         FontWeight.FromOpenTypeWeight(MathUtils.within_range(1, 999, weight_value));

      public static string InsertText(string current_text, string insertion_text, int insertion_index, int selection_start, int selection_length) {
         if (selection_length > 0)
            current_text = current_text.Remove(selection_start, selection_length);
         return InsertText(current_text, insertion_text, insertion_index);
      }

      public static string InsertText(string current_text, string insertion_text, int insertion_index) {
         if (current_text == null)
            return insertion_text;
         else if (insertion_index == current_text.Length)
            return current_text + insertion_text;
         else if (insertion_index == 0)
            return insertion_text + current_text;
         else if (insertion_index >= 0 && insertion_index < current_text.Length)
            return current_text.Substring(0, insertion_index) + insertion_text + current_text.Substring(insertion_index, current_text.Length - insertion_index);
         else return null;
      }

      public static IEnumerable<FontWeight> available_weights(this System.Windows.Media.FontFamily family) =>
          family.FamilyTypefaces
          .Select(ft => ft.Weight)
          .Distinct()
          .OrderBy(fw => fw.ToOpenTypeWeight());

      public static IEnumerable<FontWeight> all_font_weights() =>
         typeof(FontWeights).GetProperties(BindingFlags.Public | BindingFlags.Static)
         .Where(p => p.PropertyType == typeof(FontWeight))
         .Select(p => (FontWeight)p.GetValue(null)).Distinct();


      public static string name(this System.Windows.Media.FontFamily font_family) =>
         font_family.FamilyNames.Values.FirstOrDefault() ?? string.Empty;

      public static string display_name(this System.Windows.Media.FontFamily family, double size) =>
         $"{family}, {size} pt";

      public static string display_name(this System.Windows.Media.FontFamily family, FontWeight weight, double size) =>
         $"{family} {weight}, {size} pt";

      public static bool is_space_type(this char c) => CharSets.spaces.Contains(c);

      public static bool MatchAny(string text, params Regex[] valid_regexes) =>
         valid_regexes.Any(r => r.IsMatch(text));

      public static class RegexFilters {
         public static readonly Regex
            PriceInput = new Regex(@"^-?\d*((-|\.)\d*)?$"),
            Integer = new Regex(@"^\d*$");

         public static class Time {
            public static readonly Regex
               StandardTime = new Regex(@"^((0?[0-9])|(1?[0-2]?))(:?|:([0-5]?|[0-5][0-9]?)) ?(?i)((a|p)m?)?$"),
               StandardTimeOptionalSeconds = new Regex(@"^((\d?|0\d|1[0-2])|((\d|0\d|1[0-2])(:?|(:([0-5]?|[0-5]\d(( ?|( ([aApP]?|[aApP][mM])))(:?|(: ?)|(: [aApP]?[mM]?)|(:[0-5]?\d? ?[aApP]?[mM]?))|:([0-5]?|([0-5]\d( ?|( ([aApP]?|([aApP][mM]))))))))))))$"),
               MilitaryTime = new Regex(@"^(([0-1]?[0-9])|(2?[0-3]?))(:?|:([0-5]?|[0-5][0-9]?))$"),
               MilitaryTimeOptionalSeconds = new Regex(@"^((\d?|[0-1]\d|2[0-3])|((\d|[0-1]\d|2[0-3])(:?|(:([0-5]?|[0-5]\d(:?|:([0-5]?|[0-5]\d)))))))$"),
               DaysInMonth = new Regex(@"^([0-9]?|[0-2][0-9]|3[0-1])?$"),
               MonthAbbrev = new Regex(@"^(?i)(a(ug?|pr?)?|d(e|ec)?|f(e?b)?|j(an?|u[nl]?)?|m(a[ry]?)?|(o(ct?)?)|s(ep?)?|n(ov?)?)?$"),
               Year = new Regex(@"^\d{0,4}$"),
               TimeSpan = new Regex(@"^(\d+(:?|:([0-5]?|[0-5]\d(:?|:([0-5]?|[0-5]\d)))))?$");
         }
      }

      public static class StringFormats {
         public static class Time {
            public const string
               MilitaryWithSeconds = "HH:mm:ss",
               StandardWithSeconds = "hh:mm:ss tt";
         }
      }

      public static class CharSets {
         public static char[] spaces =>
            new[] { ' ', '\t', '\r', '\v' };
      }
   }
}