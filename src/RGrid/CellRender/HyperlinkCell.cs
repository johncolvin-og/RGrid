using RGrid.Utility;
using RGrid.WPF;
using System;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using FontFamily = System.Windows.Media.FontFamily;

namespace RGrid {
   public partial class DataGrid {
      public class HyperlinkCell : FrameworkElement, ICommandSource {
         // these fields each represent properties that are accessed frequently,
         // they are stored locally to reduce repetative DependencyProperty lookups
         Thickness _padding;
         HorizontalAlignment _horizontal_alignment;
         Brush _background, _foreground;
         double _glyph_width, _font_size;
         string _text;
         GlyphRun _glyph_run;
         GlyphContext _glyph_context;
         bool _is_mouse_over_text, _can_execute;
         Pen _underline_pen;

         static HyperlinkCell() =>
            WPFHelper.override_metadata<HorizontalAlignment, HyperlinkCell>(
               HorizontalAlignmentProperty,
               FrameworkPropertyMetadataOptions.AffectsArrange |
               FrameworkPropertyMetadataOptions.AffectsRender,
               (o, v) => {
                  o._horizontal_alignment = v;
                  o._glyph_run = null;
               }, HorizontalAlignment.Left);

         public HyperlinkCell() {
            _background = Background;
            _foreground = Foreground;
            _horizontal_alignment = HorizontalAlignment;
            _padding = Padding;
            _font_size = FontSize;
            _text = Text;
            _glyph_context = GlyphContext.create(FontFamily, FontWeight);
            IsMouseDirectlyOverChanged += (s, e) => {
               if (_is_mouse_over_text != IsMouseDirectlyOver)
                  _toggle_mouse_over_text();
            };
         }

         #region Background
         public static readonly DependencyProperty BackgroundProperty =
            WPFHelper.create_dp<Brush, HyperlinkCell>(
               nameof(Background),
               FrameworkPropertyMetadataOptions.AffectsRender,
               (o, v) => o._background = v,
               Brushes.Transparent);

         public Brush Background { get => GetValue(BackgroundProperty) as Brush; set => SetValue(BackgroundProperty, value); }
         #endregion

         #region Foreground
         public static readonly DependencyProperty ForegroundProperty =
            WPFHelper.create_dp<Brush, HyperlinkCell>(
               nameof(Foreground),
               FrameworkPropertyMetadataOptions.AffectsRender,
               (p, v) => {
                  p._foreground = v;
                  p._underline_pen = new Pen(v, 1);
               }, Brushes.Black);

         public Brush Foreground { get => (Brush)GetValue(ForegroundProperty); set => SetValue(ForegroundProperty, value); }
         #endregion

         #region FontWeight
         public static readonly DependencyProperty FontWeightProperty =
            TextElement.FontWeightProperty.AddOwner(
               typeof(HyperlinkCell),
               new FrameworkPropertyMetadata(
                  FontWeights.Normal,
                  FrameworkPropertyMetadataOptions.AffectsMeasure |
                  FrameworkPropertyMetadataOptions.AffectsRender,
                  WPFHelper.prop_changed_callback<FontWeight, HyperlinkCell>(
                     (p, v) => {
                        p._glyph_context = p._glyph_context.with(v);
                        p._glyph_run = null;
                     })));

         public FontWeight FontWeight { get => (FontWeight)GetValue(FontWeightProperty); set => SetValue(FontWeightProperty, value); }
         #endregion

         #region FontFamily
         public static readonly DependencyProperty FontFamilyProperty =
            TextElement.FontFamilyProperty.AddOwner(
               typeof(HyperlinkCell),
               new FrameworkPropertyMetadata(
                  TextUtils.StandardFontFamily,
                  FrameworkPropertyMetadataOptions.AffectsMeasure |
                  FrameworkPropertyMetadataOptions.AffectsRender,
                  WPFHelper.prop_changed_callback<FontFamily, HyperlinkCell>(
                     (p, v) => {
                        p._glyph_context = p._glyph_context.with(v);
                        p._glyph_run = null;
                     })));

         public FontFamily FontFamily { get => GetValue(FontFamilyProperty) as FontFamily; set => SetValue(FontFamilyProperty, value); }
         #endregion

         #region FontSize
         public static readonly DependencyProperty FontSizeProperty =
            TextElement.FontSizeProperty.AddOwner(
               typeof(HyperlinkCell),
               new FrameworkPropertyMetadata(
                  FontSizes.Small,
                  FrameworkPropertyMetadataOptions.AffectsMeasure |
                  FrameworkPropertyMetadataOptions.AffectsRender |
                  FrameworkPropertyMetadataOptions.Inherits,
               WPFHelper.prop_changed_callback<double, HyperlinkCell>(
               (p, v) => {
                  p._font_size = v;
                  p._glyph_run = null;
               })));

         public double FontSize { get => (double)GetValue(FontSizeProperty); set => SetValue(FontSizeProperty, value); }
         #endregion

         #region Padding
         public static readonly DependencyProperty PaddingProperty =
            WPFHelper.create_dp<Thickness, HyperlinkCell>(
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
            WPFHelper.create_dp<string, HyperlinkCell>(
               nameof(Text),
               FrameworkPropertyMetadataOptions.AffectsMeasure |
               FrameworkPropertyMetadataOptions.AffectsRender,
               (o, v) => {
                  o._text = v;
                  o._glyph_run = null;
               }, string.Empty);

         public string Text { get => GetValue(TextProperty) as string; set => SetValue(TextProperty, value); }
         #endregion
    
         #region ICommandSource

         #region Command
         public static readonly DependencyProperty CommandProperty =
            WPFHelper.create_dp<ICommand, HyperlinkCell>(
               nameof(Command),
               (s, old_val, new_val) => {
                  if (old_val != null)
                     CanExecuteChangedEventManager.RemoveHandler(old_val, s.OnCanExecuteChanged);
                  if (new_val != null)
                     CanExecuteChangedEventManager.AddHandler(new_val, s.OnCanExecuteChanged);
                  s.UpdateCanExecute();
               });

         [Bindable(true), Category("Action")]
         [Localizability(LocalizationCategory.NeverLocalize)]
         public ICommand Command { get => (ICommand)GetValue(CommandProperty); set => SetValue(CommandProperty, value); }
         #endregion

         #region CommandParameter
         public static readonly DependencyProperty CommandParameterProperty =
            DependencyProperty.Register(
               nameof(CommandParameter),
               typeof(object),
               typeof(HyperlinkCell));

         [Bindable(true), Category("Action")]
         [Localizability(LocalizationCategory.NeverLocalize)]
         public object CommandParameter { get => GetValue(CommandParameterProperty); set => SetValue(CommandParameterProperty, value); }
         #endregion

         #region CommandTarget
         public static readonly DependencyProperty CommandTargetProperty =
            DependencyProperty.Register(
               nameof(CommandTarget),
               typeof(IInputElement),
               typeof(HyperlinkCell));

         [Bindable(true), Category("Action")]
         public IInputElement CommandTarget { get => (IInputElement)GetValue(CommandTargetProperty); set => SetValue(CommandTargetProperty, value); }
         #endregion

         protected override bool IsEnabledCore => base.IsEnabledCore && CanExecute;

         bool CanExecute {
            get => _can_execute;
            set {
               if (_can_execute != value) {
                  _can_execute = value;
                  CoerceValue(IsEnabledProperty);
               }
            }
         }

         void OnCanExecuteChanged(object sender, EventArgs e) =>
            UpdateCanExecute();

         void UpdateCanExecute() =>
            CanExecute = Command == null || CommandHelper.CanExecuteCommandSource(this);

         #endregion

         #region Paraphrased/Simplified from https://referencesource.microsoft.com/#PresentationFramework/src/Framework/System/Windows/Documents/Hyperlink.cs

         protected override void OnMouseLeftButtonDown(MouseButtonEventArgs e) {
            base.OnMouseLeftButtonDown(e);
            _check_mouse_over_text(e);
            if (_is_mouse_over_text) {
               // Hyperlink should take focus when left mouse button is clicked on it
               // This is consistent with all ButtonBase controls and current Win32 behavior
               Focus();

               // It is possible that the mouse state could have changed during all of
               // the call-outs that have happened so far.
               if (e.ButtonState == MouseButtonState.Pressed) {
                  // Capture the mouse, and make sure we got it.
                  CaptureMouse();
               }
               e.Handled = true;
            }
         }

         protected override void OnMouseLeftButtonUp(MouseButtonEventArgs e) {
            base.OnMouseLeftButtonUp(e);
            _check_mouse_over_text(e);
            if (IsMouseCaptured)
               ReleaseMouseCapture();
            if (_can_execute && _is_mouse_over_text)
               CommandHelper.ExecuteCommandSource(this);
            e.Handled = true;
         }

         #endregion

         protected override void OnPreviewMouseMove(MouseEventArgs e) => _check_mouse_over_text(e);
         protected override void OnMouseEnter(MouseEventArgs e) => _check_mouse_over_text(e);
         protected override void OnMouseLeave(MouseEventArgs e) => _check_mouse_over_text(e);

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
            var rs = RenderSize;
            var rect = new Rect(0, 0, rs.Width, rs.Height);
            var bg = Background;
            if (bg != null)
               drawingContext.DrawRectangle(bg, null, rect);
            if (_glyph_run != null) {
               drawingContext.PushClip(new RectangleGeometry(rect));
               drawingContext.DrawGlyphRun(_foreground, _glyph_run);
               if (_is_mouse_over_text) {
                  // draw underline
                  var pt = new Point(Padding.Left, 0.5 * (ActualHeight + _font_size) + 1);
                  drawingContext.DrawLine(_underline_pen, pt, new Point(pt.X + _glyph_width, pt.Y));
               }
               drawingContext.Pop();
            }
            var bgo = GetBackgroundOverlay(this);
            if (bgo != null)
               drawingContext.DrawRectangle(bgo, null, rect);
         }

         void _check_mouse_over_text(MouseEventArgs e) {
            var pt = e.GetPosition(this);
            bool over_text =
               _glyph_run != null &&
               pt.X >= _padding.Left &&
               pt.X <= _padding.Left + _glyph_width &&
               pt.Y >= _padding.Top &&
               pt.Y <= _padding.Top + _font_size;
            if (_is_mouse_over_text != over_text)
               _toggle_mouse_over_text();
         }

         void _toggle_mouse_over_text() {
            _is_mouse_over_text = !_is_mouse_over_text;
            if (_is_mouse_over_text)
               SetCurrentValue(CursorProperty, Cursors.Hand);
            else
               ClearValue(CursorProperty);
            InvalidateVisual();
         }
      }
   }
}