using RGrid.Utility;
using RGrid.WPF;
using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
using FontFamily = System.Windows.Media.FontFamily;

namespace RGrid {
   public partial class DataGrid : Control {
      public class TextCell : FrameworkElement {
         // these fields each represent properties that are accessed frequently,
         // they are stored locally to reduce repetative DependencyProperty lookups
         Thickness _padding;
         HorizontalAlignment _horizontal_alignment;
         Brush _background, _foreground;
         double _glyph_width, _font_size;
         string _text;
         GlyphRun _glyph_run;
         GlyphContext _glyph_context;

         static TextCell() =>
            WPFHelper.override_metadata<HorizontalAlignment, TextCell>(
               HorizontalAlignmentProperty,
               FrameworkPropertyMetadataOptions.AffectsArrange |
               FrameworkPropertyMetadataOptions.AffectsRender,
               (o, v) => {
                  o._horizontal_alignment = v;
                  o._glyph_run = null;
               },
               HorizontalAlignment.Left);

         public TextCell() {
            _background = Background;
            _foreground = Foreground;
            _horizontal_alignment = HorizontalAlignment;
            _padding = Padding;
            _font_size = FontSize;
            _text = Text;
            _glyph_context = GlyphContext.create(FontFamily, FontWeight);
         }

         #region Background
         public static readonly DependencyProperty BackgroundProperty =
            WPFHelper.create_dp<Brush, TextCell>(
               nameof(Background),
               FrameworkPropertyMetadataOptions.AffectsRender,
               (p, v) => p._background = v,
               null);

         public Brush Background { get => (Brush)GetValue(BackgroundProperty); set => SetValue(BackgroundProperty, value); }
         #endregion

         #region Foreground
         public static readonly DependencyProperty ForegroundProperty =
            WPFHelper.create_dp<Brush, TextCell>(
               nameof(Foreground),
               FrameworkPropertyMetadataOptions.AffectsRender,
               (p, v) => p._foreground = v,
               Brushes.Black);

         public Brush Foreground { get => (Brush)GetValue(ForegroundProperty); set => SetValue(ForegroundProperty, value); }
         #endregion

         #region FontFamily
         public static readonly DependencyProperty FontFamilyProperty =
            TextElement.FontFamilyProperty.AddOwner(
               typeof(TextCell),
               new FrameworkPropertyMetadata(
                  TextUtils.StandardFontFamily,
                  FrameworkPropertyMetadataOptions.AffectsMeasure |
                  FrameworkPropertyMetadataOptions.AffectsRender,
                  WPFHelper.prop_changed_callback<FontFamily, TextCell>(
                     (p, v) => {
                        p._glyph_context = p._glyph_context.with(v);
                        p._glyph_run = null;
                     })));

         public FontFamily FontFamily { get => GetValue(FontFamilyProperty) as FontFamily; set => SetValue(FontFamilyProperty, value); }
         #endregion

         #region FontWeight
         public static readonly DependencyProperty FontWeightProperty =
            TextElement.FontWeightProperty.AddOwner(
               typeof(TextCell),
               new FrameworkPropertyMetadata(
                  FontWeights.Normal,
                  FrameworkPropertyMetadataOptions.AffectsMeasure |
                  FrameworkPropertyMetadataOptions.AffectsRender,
                  WPFHelper.prop_changed_callback<FontWeight, TextCell>(
                     (p, v) => {
                        p._glyph_context = p._glyph_context.with(v);
                        p._glyph_run = null;
                     })));

         public FontWeight FontWeight { get => (FontWeight)GetValue(FontWeightProperty); set => SetValue(FontWeightProperty, value); }
         #endregion

         #region FontSize
         public static readonly DependencyProperty FontSizeProperty =
            TextElement.FontSizeProperty.AddOwner(
               typeof(TextCell),
               new FrameworkPropertyMetadata(
                  FontSizes.Small,
                  FrameworkPropertyMetadataOptions.AffectsMeasure |
                  FrameworkPropertyMetadataOptions.AffectsRender |
                  FrameworkPropertyMetadataOptions.Inherits,
               WPFHelper.prop_changed_callback<double, TextCell>(
               (p, v) => {
                  p._font_size = v;
                  p._glyph_run = null;
               })));

         public double FontSize { get => (double)GetValue(FontSizeProperty); set => SetValue(FontSizeProperty, value); }
         #endregion

         #region Padding
         public static readonly DependencyProperty PaddingProperty =
            WPFHelper.create_dp<Thickness, TextCell>(
               nameof(Padding),
               FrameworkPropertyMetadataOptions.AffectsMeasure |
               FrameworkPropertyMetadataOptions.AffectsRender,
               (p, v) => {
                  p._padding = v;
                  p._glyph_run = null;
               }, DefaultCellPadding);

         public Thickness Padding { get => (Thickness)GetValue(PaddingProperty); set => SetValue(PaddingProperty, value); }
         #endregion

         #region Text
         public static readonly DependencyProperty TextProperty =
            WPFHelper.create_dp<string, TextCell>(
               nameof(Text),
               FrameworkPropertyMetadataOptions.AffectsMeasure |
               FrameworkPropertyMetadataOptions.AffectsRender,
               (p, v) => {
                  p._text = v;
                  p._glyph_run = null;
               });

         public string Text { get => GetValue(TextProperty) as string; set => SetValue(TextProperty, value); }
         #endregion

         protected override Size ArrangeOverride(Size finalSize) {
            if (_glyph_run == null)
               _set_glyph(finalSize);
            return finalSize;
         }

         protected override Size MeasureOverride(Size availableSize) {
            if (_glyph_run == null)
               _set_glyph(availableSize);
            return new Size(Math.Max(availableSize.Width, _padding.Left + _glyph_width + _padding.Right),
               Math.Max(availableSize.Height, _padding.Top + _font_size + _padding.Bottom));
         }

         void _set_glyph(Size size) {
            _glyph_run = _glyph_context.get_glyph_run(_text, _font_size, size, _horizontal_alignment, VerticalAlignment.Center, _padding);
            _glyph_width = _glyph_run == null ? 0 : _glyph_run.AdvanceWidths.Sum();
         }

         protected override void OnRender(DrawingContext drawingContext) {
            if (_background != null)
               drawingContext.DrawRectangle(_background, null, new Rect(RenderSize));
            if (_glyph_run != null)
               drawingContext.DrawGlyphRun(_foreground, _glyph_run);
         }
      }
   }
}