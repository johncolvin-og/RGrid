using Monitor.Render.Utilities;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Media;

namespace RGrid.Utility {
   /// Reduces the performance overhead of creating GlyphRuns of a particular GlyphTypeface by mapping all chars to glyph indices/widths upon construction.
   /// <para/>Note: Useful in situations where Glyphtypeface will never or rarely change.  The overhead, in this case, is spent creating the map in the constructor.
   /// </summary>
   public class GlyphContext {
      static readonly GlyphTypeface _default_glyph_typeface;
      static readonly ConcurrentDictionary<GlyphTypeface, Lazy<ReadOnlyDictionary<int, (ushort index, double width)>>> _glyph_char_maps =
        new ConcurrentDictionary<GlyphTypeface, Lazy<ReadOnlyDictionary<int, (ushort index, double width)>>>();

      readonly GlyphTypeface _glyph_typeface;
      readonly IDictionary<int, (ushort index, double width)> _char_map;

      static GlyphContext() =>
         TextUtils.StandardTypeface.TryGetGlyphTypeface(out _default_glyph_typeface);

      GlyphContext(GlyphTypeface glyph_typeface) {
         _glyph_typeface = glyph_typeface;
         _char_map = _get_glyph_char_map(glyph_typeface);
      }

      public static GlyphContext create(string font_family, int font_weight) =>
         create(font_family, TextUtils.from_open_type_weight_safe(font_weight));

      public static GlyphContext create(string font_family, FontWeight font_weight) =>
         create(new System.Windows.Media.FontFamily(font_family), font_weight);

      public static GlyphContext create(System.Windows.Media.FontFamily font_family, FontWeight font_weight) {
         var typeface = new Typeface(font_family, FontStyles.Normal, font_weight, FontStretches.Normal);
         return new GlyphContext(typeface.TryGetGlyphTypeface(out GlyphTypeface gt) ? gt : _default_glyph_typeface);
      }

      public static GlyphContext create(GlyphTypeface glyph_typeface) =>
         new GlyphContext(glyph_typeface);

      public GlyphContext with(FontWeight font_weight) {
         string family = _glyph_typeface.FamilyNames.FirstOrDefault().Value;
         if (string.IsNullOrEmpty(family)) // idk how this would happen, but just to be safe
            family = TextUtils.StandardFontFamilyName;
         return create(new System.Windows.Media.FontFamily(family), font_weight);
      }

      public GlyphContext with(System.Windows.Media.FontFamily font_family) =>
         create(font_family, _glyph_typeface.Weight);

      public GlyphRun get_glyph_run(
         string text, double font_size, double available_width, double available_height,
         HorizontalAlignment x_alignment, double x_offset, VerticalAlignment y_alignment, double y_offset, bool use_layout_rounding = false, bool is_sideways = false) {
         //
         if (string.IsNullOrEmpty(text))
            return null;
         var glyph_indices = new ushort[text.Length];
         var advance_widths = new double[text.Length];
         if (!_try_get_glyph_params(text, font_size, ref glyph_indices, ref advance_widths, out double glyph_width))
            return null;
         var origin = _calculate_glyph_run_origin(font_size, available_width, available_height, glyph_width, x_alignment, x_offset, y_alignment, y_offset, use_layout_rounding);
         return new GlyphRun(_glyph_typeface, 0, is_sideways, font_size, DPIUtils.fdpi_ratio, glyph_indices, origin, advance_widths, null, null, null, null, null, null);
      }

      public GlyphRun get_glyph_run(string text, double font_size, Size available_size, HorizontalAlignment x_alignment, VerticalAlignment y_alignment, Thickness padding) =>
         get_glyph_run(text, font_size, available_size.Width, available_size.Height, x_alignment, y_alignment, padding);

      public GlyphRun get_glyph_run(string text, double font_size, double available_width, double available_height, HorizontalAlignment x_alignment, VerticalAlignment y_alignment, Thickness padding) =>
         get_glyph_run(text, font_size, 0, 0, available_width, available_height, x_alignment, y_alignment, padding);

      public GlyphRun get_glyph_run(
         string text, double font_size, double left, double top, double right, double bottom,
         HorizontalAlignment x_alignment, VerticalAlignment y_alignment, Thickness padding) {
         //
         if (string.IsNullOrEmpty(text))
            return null;
         ushort[] glyph_indices = new ushort[text.Length];
         double[] advance_widths = new double[text.Length];
         if (!_try_get_glyph_params(text, font_size, ref glyph_indices, ref advance_widths, out double glyph_width))
            return null;
         var origin = _calculate_glyph_run_origin(font_size, left, top, right, bottom, glyph_width, x_alignment, y_alignment, padding);
         return new GlyphRun(_glyph_typeface, 0, false, font_size, DPIUtils.fdpi_ratio, glyph_indices, origin, advance_widths, null, null, null, null, null, null);
      }

      public GlyphRun get_glyph_run_wrapped(
         string text, double font_size, double line_spacing, double left, double top, double right, double bottom,
         HorizontalAlignment x_alignment, VerticalAlignment y_alignment, Thickness padding) {
         //
         if (string.IsNullOrEmpty(text))
            return null;
         ushort[] glyph_indices = new ushort[text.Length];
         double[] advance_widths = new double[text.Length];
         if (!_try_get_glyph_params(text, font_size, ref glyph_indices, ref advance_widths, out double glyph_width))
            return null;
         double allowed_width = right - left - padding.Left - padding.Right;
         double adj_allowed_width = allowed_width / font_size;
         double glyph_height = font_size;
         Point[] glyph_offsets = null;
         if (glyph_width > allowed_width) {
            List<(int n_chars, double line_width)> chars_per_line = new List<(int n_chars, double line_width)>();
            int line_start_pos = 0;
            int pos = 0;
            while (line_start_pos < advance_widths.Length) {
               double line_width = 0;
               while (line_width < allowed_width && pos < advance_widths.Length) {
                  line_width += advance_widths[pos++];
               }
               chars_per_line.Add((pos - line_start_pos, line_width));
               line_start_pos = pos;
            }
            glyph_offsets = new Point[glyph_indices.Length];
            double x_offset = 0, y_offset = 0;
            pos = 0;
            foreach ((int n_chars, double line_width) in chars_per_line) {
               y_offset += (font_size + line_spacing);
               int line_end_pos = pos + n_chars;
               for (; pos < line_end_pos; pos++)
                  glyph_offsets[pos] = new Point(x_offset, y_offset);
            }
            glyph_height = chars_per_line.Count * (font_size + line_spacing) - line_spacing;
         }
         var origin = _calculate_glyph_run_origin(font_size, left, top, right, bottom, glyph_width, x_alignment, y_alignment, padding);
         return new GlyphRun(_glyph_typeface, 0, false, font_size, DPIUtils.fdpi_ratio, glyph_indices, origin, advance_widths, null, null, null, null, null, null);
      }

      static Point _calculate_glyph_run_origin(
         double font_size, double available_width, double available_height, double glyph_width,
         HorizontalAlignment x_alignment, double x_offset, VerticalAlignment y_alignment, double y_offset, bool use_layout_rounding = false) {
         //
         double x, y;
         switch (x_alignment) {
            case HorizontalAlignment.Left: x = 0; break;
            case HorizontalAlignment.Right: x = available_width - glyph_width; break;
            case HorizontalAlignment.Center:
            default: x = 0.5 * (available_width - glyph_width); break;
         }
         x += x_offset;
         switch (y_alignment) {
            case VerticalAlignment.Top: y = font_size; break;
            case VerticalAlignment.Bottom: y = available_height - font_size; break;
            case VerticalAlignment.Center:
            default: y = available_height - 0.5 * (available_height - font_size); break;
         }
         y += y_offset;
         if (use_layout_rounding) {
            x = Math.Round(x);
            y = Math.Round(y);
         }
         return new Point(x, y);
      }

      static Point _calculate_glyph_run_origin(
        double font_size, double left, double top, double right, double bottom, double glyph_width,
        HorizontalAlignment x_alignment, VerticalAlignment y_alignment, Thickness padding) {
         //
         double available_width = right - left, available_height = bottom - top;
         double x;
         switch (x_alignment) {
            case HorizontalAlignment.Left: x = left + padding.Left; break;
            case HorizontalAlignment.Right: x = right - glyph_width - padding.Right; break;
            case HorizontalAlignment.Center:
            default:
               // ignore padding if it is Center/Stretch aligned
               x = left + 0.5 * (available_width - glyph_width);
               break;
         }
         double y;
         switch (y_alignment) {
            case VerticalAlignment.Top: y = top + font_size + padding.Top; break;
            case VerticalAlignment.Bottom: y = bottom - font_size - padding.Bottom; break;
            case VerticalAlignment.Center:
            default:
               // ignore padding if it is Center/Stretch aligned
               y = available_height - 0.5 * (available_height - font_size);
               break;
         }
         return new Point(x, y);
      }

      bool _try_get_glyph_params(string text, double font_size, ref ushort[] glyph_indices, ref double[] advance_widths, out double glyph_width) {
         glyph_width = 0;
         for (int n = 0; n < text.Length; n++) {
            if (!_char_map.TryGetValue(text[n], out (ushort index, double width) gc))
               return false;
            glyph_indices[n] = gc.index;
            double w = gc.width * font_size;
            advance_widths[n] = w;
            glyph_width += w;
         }
         return true;
      }

      #region MultiLine
      public bool arrange_glyph_run(
         IReadOnlyList<string> lines, double line_spacing, TextAlignment text_alignment, double font_size, double max_width,
         out ushort[] glyph_indices, out double[] advance_widths, out Point[] glyph_offsets, out double glyph_width, out double glyph_height) {
         //
         glyph_width = 0;
         glyph_height = 0;
         if (lines.is_null_or_empty()) {
            glyph_indices = new ushort[0];
            advance_widths = new double[0];
            glyph_offsets = new Point[0];
            return false;
         }
         int size = lines.Sum(s => s.Length);
         glyph_indices = new ushort[size];
         advance_widths = new double[size];
         glyph_offsets = new Point[size];
         switch (text_alignment) {
            case TextAlignment.Left:
               return _try_get_glyph_params_left_aligned(lines, line_spacing, font_size, max_width, ref glyph_indices, ref advance_widths, ref glyph_offsets, out glyph_width, out glyph_height);
            case TextAlignment.Center: // TODO
            case TextAlignment.Right: // TODO
            default: goto case TextAlignment.Left;
         }
      }

      public bool arrange_glyph_run(
         IReadOnlyList<string> lines, double line_spacing, TextAlignment text_alignment, double font_size,
         out ushort[] glyph_indices, out double[] advance_widths, out Point[] glyph_offsets, out double glyph_width, out double glyph_height) {
         //
         glyph_width = 0;
         glyph_height = 0;
         if (lines.is_null_or_empty()) {
            glyph_indices = new ushort[0];
            advance_widths = new double[0];
            glyph_offsets = new Point[0];
            return false;
         }
         int size = lines.Sum(s => s.Length);
         glyph_indices = new ushort[size];
         advance_widths = new double[size];
         glyph_offsets = new Point[size];
         switch (text_alignment) {
            case TextAlignment.Left:
               return _try_get_glyph_params_left_aligned(lines, line_spacing, font_size, ref glyph_indices, ref advance_widths, ref glyph_offsets, out glyph_width, out glyph_height);
            case TextAlignment.Center: // TODO
            case TextAlignment.Right: // TODO
            default: goto case TextAlignment.Left;
         }
      }

      public GlyphRun from_arranged(double font_size, Point origin, ushort[] indices, double[] advance_widths, Point[] offsets) =>
         new GlyphRun(_glyph_typeface, 0, false, font_size, DPIUtils.fdpi_ratio, indices, origin, advance_widths, offsets, null, null, null, null, null);

      bool _try_get_glyph_params_left_aligned(
         IReadOnlyList<string> lines, double line_spacing, double font_size, ref ushort[] glyph_indices, ref double[] advance_widths, ref Point[] glyph_offsets, out double glyph_width, out double glyph_height) {
         //
         glyph_width = 0;
         glyph_height = 0;
         double x_offset = 0;
         int n = 0;
         foreach (string text in lines) {
            double line_width = 0;
            foreach (char c in text) {
               if (!_char_map.TryGetValue(c, out (ushort index, double width) gc))
                  return false;
               glyph_indices[n] = gc.index;
               double w = gc.width * font_size;
               advance_widths[n] = w;
               line_width += w;
               glyph_offsets[n] = new Point(x_offset, glyph_height);
               n++;
            }
            x_offset -= line_width;
            if (line_width > glyph_width)
               glyph_width = line_width;
            glyph_height += (font_size + line_spacing);
         }
         glyph_height -= line_spacing; // remove the last line space
         return true;
      }

      bool _try_get_glyph_params_left_aligned(
         IReadOnlyList<string> lines, double line_spacing, double font_size, double max_width,
         ref ushort[] glyph_indices, ref double[] advance_widths, ref Point[] glyph_offsets, out double glyph_width, out double glyph_height) {
         //
         glyph_width = 0;
         glyph_height = 0;
         double x_offset = 0;
         int pos = 0;
         List<char> last_word = new List<char>();
         foreach (string line in lines) {
            double line_width = 0;
            int char_index = 0, word_length = 0;
            foreach (char c in line) {
               if (!_char_map.TryGetValue(c, out (ushort index, double width) gc))
                  return false;
               double char_width = gc.width * font_size;
               advance_widths[pos] = char_width;
               glyph_indices[pos] = gc.index;
               if (c.is_space_type()) {
                  word_length = 0;
               } else if (char_width > (max_width - line_width)) {
                  glyph_height += (font_size + line_spacing);
                  // wrap
                  double word_width = 0;
                  foreach (int oi in Enumerable.Range(char_index - word_length, word_length))
                     word_width += advance_widths[oi];
                  x_offset -= (line_width - word_width);
                  foreach (int oi in Enumerable.Range(char_index - word_length, word_length)) {
                     var curr = glyph_offsets[oi];
                     glyph_offsets[oi] = new Point(x_offset, -glyph_height);
                  }
                  line_width = word_width;
               } else {
                  ++word_length;
               }
               line_width += char_width;
               glyph_offsets[pos++] = new Point(x_offset, -glyph_height);
               ++char_index;
            }
            x_offset -= line_width;
            if (line_width > glyph_width)
               glyph_width = line_width;
            glyph_height += (font_size + line_spacing);
         }
         glyph_height -= line_spacing; // remove the last line space
         return true;
      }

      bool _try_get_glyph_params_left_aligned(
         IReadOnlyList<string[]> line_words, double line_spacing, double font_size, double max_width,
         ref ushort[] glyph_indices, ref double[] advance_widths, ref Point[] glyph_offsets, out double glyph_width, out double glyph_height) {
         //
         glyph_width = 0;
         glyph_height = 0;
         double x_offset = 0;
         int n = 0;
         for (int li = 0; li < line_words.Count; li++) {
            double line_width = 0;
            foreach (string w in line_words[li]) {
               //if (w )
               for (int ci = 0; ci < w.Length; ci++) {
                  if (!_char_map.TryGetValue(w[ci], out (ushort index, double width) gc))
                     return false;
                  glyph_indices[n] = gc.index;
                  double char_width = gc.width * font_size;
                  advance_widths[n] = char_width;
                  if (char_width > (max_width - line_width)) {
                     // wrap
                     glyph_height += (font_size + line_spacing);
                     x_offset -= line_width;
                     line_width = 0;
                     n -= ci;
                     ci = -1;
                     continue;
                  } else {
                     line_width += char_width;
                  }
                  glyph_offsets[n] = new Point(x_offset, -glyph_height);
                  n++;
               }
            }
            x_offset -= line_width;
            if (line_width > glyph_width)
               glyph_width = line_width;
            glyph_height += (font_size + line_spacing);
         }
         glyph_height -= line_spacing; // remove the last line space
         return true;
      }
      #endregion

      static ReadOnlyDictionary<int, (ushort index, double width)> _get_glyph_char_map(GlyphTypeface glyph_typeface) =>
         _glyph_char_maps.GetOrAdd(glyph_typeface, gt => new Lazy<ReadOnlyDictionary<int, (ushort index, double width)>>(() => {
            var widths = glyph_typeface.AdvanceWidths;
            return new ReadOnlyDictionary<int, (ushort, double)>(
               glyph_typeface.CharacterToGlyphMap.ToDictionary(kv => kv.Key, kv => (kv.Value, widths[kv.Value])));
         })).Value;
   }
}
