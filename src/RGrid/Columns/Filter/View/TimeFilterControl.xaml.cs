using RGrid.Filters;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
using RGrid.WPF;

namespace RGrid.Controls {
   /// <summary>
   /// Interaction logic for TimeFilterControl.xaml
   /// </summary>
   public class TimeFilterControl : Control {
      static TimeFilterControl() =>
         DefaultStyleKeyProperty.OverrideMetadata(typeof(TimeFilterControl), new FrameworkPropertyMetadata(typeof(TimeFilterControl)));

      public TimeFilterControl() =>
         new TimeFilterErrorAdorner(this);

      public static IEnumerable<TimeFilterMode> ModeItems {
         get {
            yield return TimeFilterMode.Anytime;
            yield return TimeFilterMode.Today;
            yield return TimeFilterMode.CMEOpen;
            yield return TimeFilterMode.Period;
         }
      }

      public static readonly DependencyProperty ModeProperty = DependencyProperty.Register("Mode", typeof(TimeFilterMode), typeof(TimeFilterControl),
         new FrameworkPropertyMetadata(TimeFilterMode.Anytime, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault));
      public TimeFilterMode Mode { get => (TimeFilterMode)GetValue(ModeProperty); set => SetValue(ModeProperty, value); }

      public static readonly DependencyProperty BeginProperty = DependencyProperty.Register("Begin", typeof(TimeTextTuple), typeof(TimeFilterControl),
         new FrameworkPropertyMetadata(default(TimeTextTuple), FrameworkPropertyMetadataOptions.BindsTwoWayByDefault));
      public TimeTextTuple Begin { get => (TimeTextTuple)GetValue(BeginProperty); set => SetValue(BeginProperty, value); }

      public static readonly DependencyProperty EndProperty = DependencyProperty.Register("End", typeof(TimeTextTuple), typeof(TimeFilterControl),
         new FrameworkPropertyMetadata(default(TimeTextTuple), FrameworkPropertyMetadataOptions.BindsTwoWayByDefault));
      public TimeTextTuple End { get => (TimeTextTuple)GetValue(EndProperty); set => SetValue(EndProperty, value); }

      public override void OnApplyTemplate() {
         var clear_begin_time = this.assert_template_child<Hyperlink>("ClearBeginLink");
         var clear_end_time = this.assert_template_child<Hyperlink>("ClearEndLink");
         var set_last_hour = this.assert_template_child<Hyperlink>("SetLastHourLink");
         clear_begin_time.Command = new DelegateCommand(() => Begin = default(TimeTextTuple));
         clear_end_time.Command = new DelegateCommand(() => End = default(TimeTextTuple));
         set_last_hour.Command = new DelegateCommand(() => Begin = TimeTextTuple.FromDateTime(DateTime.Now.AddHours(-1)));
      }

      class TimeFilterErrorAdorner : NotifyDataErrorAdorner<TimeFilterControl, ITimeFilterVM>, IDisposable {
         readonly ChildProperty<DateTimePicker> _begin_picker, _end_picker;

         public TimeFilterErrorAdorner(TimeFilterControl view) : base(view) {
            _begin_picker = child_property<DateTimePicker>("PART_Begin_DateTimePicker");
            _end_picker = child_property<DateTimePicker>("PART_End_DateTimePicker");
         }

         protected override void OnRender(DrawingContext drawing_context) {
            const double x_offset = 99, y_offset = -3;
            if (_vm.GetErrors(nameof(ITimeFilterVM.begin)).Cast<string>().FirstOrDefault() is string begin_msg) {
               this.draw_border(drawing_context, _begin_picker.value, null, new Pen(Brushes.Red, 1));
               this.draw_relative_text(RelativePosition.Above, begin_msg, drawing_context, _begin_picker.value, Brushes.Red, x_offset, y_offset);
            }
            if (_vm.GetErrors(nameof(ITimeFilterVM.end)).Cast<string>().FirstOrDefault() is string end_msg) {
               this.draw_border(drawing_context, _end_picker.value, null, new Pen(Brushes.Red, 1));
               this.draw_relative_text(RelativePosition.Above, end_msg, drawing_context, _end_picker.value, Brushes.Red, x_offset, y_offset);
            }
         }
      }
   }

   public enum TimeFilterMode {
      Anytime,
      Today,
      [Description("Period:")]
      Period,
      [Description("Since Open")]
      CMEOpen
   }
}