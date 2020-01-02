using Disposable.Extensions;
using Disposable.Extensions.Utilities;
using RGrid.WPF;
using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;

namespace RGrid.Controls {
   /// <summary>
   /// Interaction logic for PopupControl.xaml
   /// </summary>
   public class PopupControl : HeaderedContentControl {
      static PopupControl() =>
         DefaultStyleKeyProperty.OverrideMetadata(typeof(PopupControl), new FrameworkPropertyMetadata(typeof(PopupControl)));

      public static readonly RoutedCommand
         OpenCommand = new RoutedCommand("Open", typeof(PopupControl)),
         CloseCommand = new RoutedCommand("Close", typeof(PopupControl));

      public static readonly RoutedEvent OpenedEvent = EventManager.RegisterRoutedEvent(nameof(Opened), RoutingStrategy.Bubble, typeof(EventHandler), typeof(PopupControl));
      public static readonly RoutedEvent ClosedEvent = EventManager.RegisterRoutedEvent(nameof(Closed), RoutingStrategy.Bubble, typeof(EventHandler), typeof(PopupControl));

      public event RoutedEventHandler Opened { add => AddHandler(OpenedEvent, value); remove => RemoveHandler(OpenedEvent, value); }
      public event RoutedEventHandler Closed { add => AddHandler(ClosedEvent, value); remove => RemoveHandler(ClosedEvent, value); }

      #region PopupPlacement
      public static readonly DependencyProperty PopupPlacementProperty = DependencyProperty.Register("PopupPlacement", typeof(PlacementMode), typeof(PopupControl), new PropertyMetadata(PlacementMode.Right, OnPopupPlacementChanged));
      private static void OnPopupPlacementChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) { d.SetValue(IsOpenTransformPropertyKey, _get_is_open_transform((PlacementMode)e.NewValue)); }
      public PlacementMode PopupPlacement { get => (PlacementMode)GetValue(PopupPlacementProperty); set => SetValue(PopupPlacementProperty, value); }
      #endregion

      #region HorizontalOffset
      public static readonly DependencyProperty HorizontalOffsetProperty = DependencyProperty.Register(nameof(HorizontalOffset), typeof(double), typeof(PopupControl), new PropertyMetadata(0.0, OnHorizontalOffsetChanged));
      private static void OnHorizontalOffsetChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) {
         var target = (PopupControl)d;
         if (target._popup != null) target._popup.HorizontalOffset = (double)e.NewValue;
      }
      public double HorizontalOffset { get => (double)GetValue(HorizontalOffsetProperty); set => SetValue(HorizontalOffsetProperty, value); }
      #endregion

      #region VerticalOffset
      public static readonly DependencyProperty VerticalOffsetProperty = DependencyProperty.Register(nameof(VerticalOffset), typeof(double), typeof(PopupControl), new PropertyMetadata(0.0, OnVerticalOffsetChanged));
      private static void OnVerticalOffsetChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) {
         var target = (PopupControl)d;
         if (target._popup != null) target._popup.VerticalOffset = (double)e.NewValue;
      }
      public double VerticalOffset { get => (double)GetValue(VerticalOffsetProperty); set => SetValue(VerticalOffsetProperty, value); }
      #endregion

      #region PlacementTarget
      public static readonly DependencyProperty PlacementTargetProperty = DependencyProperty.Register(nameof(PlacementTarget), typeof(UIElement), typeof(PopupControl), new PropertyMetadata(null, OnPlacementTargetChanged));
      private static void OnPlacementTargetChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) {
         var target = (PopupControl)d;
         if (target._popup != null) target._popup.PlacementTarget = e.NewValue as UIElement ?? target._button;
      }
      public UIElement PlacementTarget { get => GetValue(PlacementTargetProperty) as UIElement; set => SetValue(PlacementTargetProperty, value); }
      #endregion

      #region ShowExpanderArrow
      public static readonly DependencyProperty ShowExpanderArrowProperty = DependencyProperty.Register(nameof(ShowExpanderArrow), typeof(bool), typeof(PopupControl), new PropertyMetadata(true));
      public bool ShowExpanderArrow { get => (bool)GetValue(ShowExpanderArrowProperty); set => SetValue(ShowExpanderArrowProperty, value); }
      #endregion

      #region PopupContainerStyle
      public static readonly DependencyProperty PopupContainerStyleProperty = DependencyProperty.Register("PopupContainerStyle", typeof(Style), typeof(PopupControl));
      public Style PopupContainerStyle { get => GetValue(PopupContainerStyleProperty) as Style; set => SetValue(PopupContainerStyleProperty, value); }
      #endregion

      #region IsOpen
      public static readonly DependencyProperty IsOpenProperty = DependencyProperty.Register("IsOpen", typeof(bool), typeof(PopupControl),
         new FrameworkPropertyMetadata(false, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, OnIsOpenChanged));
      static void OnIsOpenChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) {
         var target = (PopupControl)d;
         if ((bool)e.NewValue) {
            target.RaiseEvent(new RoutedEventArgs(OpenedEvent, target));
            (FocusManager.GetFocusScope(target) as UIElement)?.Focus();
            Keyboard.Focus(target);
            target?._popup?.Child?.Focus();
         } else {
            target.ReleaseMouseCapture();
            target.RaiseEvent(new RoutedEventArgs(ClosedEvent, target));
            if (target.IsKeyboardFocused || target.IsKeyboardFocusWithin)
               Keyboard.ClearFocus();
         }
      }
      public bool IsOpen { get => (bool)GetValue(IsOpenProperty); set => SetValue(IsOpenProperty, value); }
      #endregion

      #region IsOpenTransform (ReadOnly)
      private static readonly DependencyPropertyKey IsOpenTransformPropertyKey = DependencyProperty.RegisterReadOnly("IsOpenTransform", typeof(ScaleTransform), typeof(PopupControl),
         new PropertyMetadata(_get_is_open_transform((PlacementMode)PopupPlacementProperty.DefaultMetadata.DefaultValue)));
      internal static readonly DependencyProperty IsOpenTransformProperty = IsOpenTransformPropertyKey.DependencyProperty;
      public ScaleTransform IsOpenTransform { get => GetValue(IsOpenTransformProperty) as ScaleTransform; set => SetValue(IsOpenTransformPropertyKey, value); }

      private static ScaleTransform _get_is_open_transform(PlacementMode popup_placement) {
         switch (popup_placement) {
            case PlacementMode.Left: return new ScaleTransform(-1, 1, 1.75, 0);
            case PlacementMode.Right: return new ScaleTransform(-1, 1, 1.75, 0);
            case PlacementMode.Top: return new ScaleTransform(1, -1, 0, 1.75);
            case PlacementMode.Bottom: return new ScaleTransform(1, -1, 0, 1.75);
            default: return null;
         }
      }
      #endregion

      #region TogglePopupCommand (ReadOnly)
      private static readonly DependencyPropertyKey TogglePopupCommandPropertyKey = DependencyProperty.RegisterReadOnly("TogglePopupCommand", typeof(ICommand), typeof(PopupControl), new PropertyMetadata());
      internal static readonly DependencyProperty TogglePopupCommandProperty = TogglePopupCommandPropertyKey.DependencyProperty;
      internal ICommand TogglePopupCommand { get => GetValue(TogglePopupCommandProperty) as ICommand; private set => SetValue(TogglePopupCommandPropertyKey, value); }
      #endregion

      public PopupControl() {
         TogglePopupCommand = new DelegateCommand(() => {
            if (_ignore_open_command) _ignore_open_command = false;
            else IsOpen = true;
         });
         CommandBindings.Add(new CommandBinding(OpenCommand, (s, e) => IsOpen = true));
         CommandBindings.Add(new CommandBinding(CloseCommand, (s, e) => IsOpen = false));
      }

      private IDisposable _popup_hook;
      private Popup _popup;
      private Button _button;
      private bool _ignore_open_command;

      public override void OnApplyTemplate() {
         DisposableUtils.Dispose(ref _popup_hook);
         _button = (Button)GetTemplateChild("button");
         _popup = (Popup)GetTemplateChild("popup");
         _popup.HorizontalOffset = HorizontalOffset;
         _popup.VerticalOffset = VerticalOffset;
         _popup.PlacementTarget = PlacementTarget ?? _button;
         _popup.Closed += _popup_Closed;
         _popup_hook = DisposableFactory.Create(() => _popup.Closed -= _popup_Closed);
      }

      private void _popup_Closed(object sender, EventArgs e) {
         if (_button.IsMouseOver && Mouse.LeftButton == MouseButtonState.Pressed)
            _ignore_open_command = true;
      }
   }
}