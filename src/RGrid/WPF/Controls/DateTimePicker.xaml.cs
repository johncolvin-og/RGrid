using RGrid.WPF;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using static RGrid.Utility.TextUtils;

namespace RGrid.Controls {
   /// <summary>
   /// Interaction logic for DateTimePicker.xaml
   /// </summary>
   public class DateTimePicker : Control {
      public static readonly RoutedCommand TickTimeComponentCommand = new RoutedCommand();

      DTPController _controller;
      bool _suspend_notify_vm;

      static DateTimePicker() =>
         DefaultStyleKeyProperty.OverrideMetadata(typeof(DateTimePicker), new FrameworkPropertyMetadata(typeof(DateTimePicker)));

      public static DependencyProperty TimeTupleProperty = DependencyProperty.Register("TimeTuple", typeof(TimeTextTuple), typeof(DateTimePicker),
         new FrameworkPropertyMetadata(default(TimeTextTuple), FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, OnTimeTupleChanged));
      static void OnTimeTupleChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) {
         var dtp = (DateTimePicker)d;
         if (!dtp._suspend_notify_vm && dtp._controller != null)
            dtp._controller.result = (TimeTextTuple)e.NewValue;
      }
      public TimeTextTuple TimeTuple { get => (TimeTextTuple)GetValue(TimeTupleProperty); set => SetValue(TimeTupleProperty, value); }

      IList<IDisposable> _hooks = new List<IDisposable>();

      public override void OnApplyTemplate() {
         var month = this.assert_template_child<TickUpDown>("PART_month_tick");
         var month_text = this.assert_template_child<TextBox>("PART_month_text");
         var day = this.assert_template_child<TickUpDown>("PART_day_tick");
         var day_text = this.assert_template_child<TextBox>("PART_day_text");
         var year = this.assert_template_child<TickUpDown>("PART_year_tick");
         var year_text = this.assert_template_child<TextBox>("PART_year_text");
         var time = this.assert_template_child<TickUpDown>("PART_time_tick");
         var time_text = this.assert_template_child<TextBox>("PART_time_text");

         if (_controller != null) _controller.PropertyChanged -= _vm_PropertyChanged;
         _controller = new DTPController(time_text);
         _controller.result = TimeTuple;
         _controller.PropertyChanged += _vm_PropertyChanged;
         time.CommandBindings.Add(new CommandBinding(TickTimeComponentCommand, (s, e) => {
            _controller.tick_time_component((int)e.Parameter);
         }));
         time.CommandBindings.Add(new CommandBinding(TextBoxBehavior.SelectAllCommand, TextBoxBehavior.SelectAllCommandExecutedHandler));

         const int delay = 500;
         time.SetBinding(TickUpDown.TickCommandProperty, new Binding("tick_time") { Source = _controller });
         time.TickUpParameter = 1;
         time.TickDownParameter = -1;
         time.TickUpAltParameter = 60;
         time.TickDownAltParameter = -60;
         time_text.SetBinding(TextBox.TextProperty, new Binding("time_text") { Source = _controller, Delay = delay, ValidatesOnExceptions = true });
         _hooks.Add(time_text.SubscribePreviewTextInput((s, e) => {
            string preview = time_text.PeekText(e);
            if (RegexFilters.Time.StandardTimeOptionalSeconds.IsMatch(preview)) {
               _controller.use_military_time = false;
            } else if (RegexFilters.Time.MilitaryTimeOptionalSeconds.IsMatch(preview)) {
               _controller.use_military_time = true;
            } else {
               e.Handled = true;
            }
         }));

         month.SetBinding(TickUpDown.TickCommandProperty, new Binding("tick_month") { Source = _controller });
         month.TickUpParameter = 1;
         month.TickDownParameter = -1;
         month_text.SetBinding(TextBox.TextProperty, new Binding("month_text") { Source = _controller, Delay = delay });
         _hooks.Add(month_text.SubscribePreviewTextInput((s, e) =>
            e.Handled = !RegexFilters.Time.MonthAbbrev.IsMatch(month_text.PeekText(e))
         ));

         day.SetBinding(TickUpDown.TickCommandProperty, new Binding("tick_day") { Source = _controller });
         day.TickUpParameter = 1;
         day.TickDownParameter = -1;
         day_text.SetBinding(TextBox.TextProperty, new Binding("day_text") { Source = _controller, Delay = delay });
         _hooks.Add(day_text.SubscribePreviewTextInput((s, e) =>
            e.Handled = !RegexFilters.Time.DaysInMonth.IsMatch(day_text.PeekText(e))
         ));

         year.SetBinding(TickUpDown.TickCommandProperty, new Binding("tick_year") { Source = _controller });
         year.TickUpParameter = 1;
         year.TickDownParameter = -1;
         year_text.SetBinding(TextBox.TextProperty, new Binding("year_text") { Source = _controller, Delay = delay });
         _hooks.Add(year_text.SubscribePreviewTextInput((s, e) =>
            e.Handled = !RegexFilters.Time.Year.IsMatch(year_text.PeekText(e))
         ));
      }

      void _vm_PropertyChanged(object sender, PropertyChangedEventArgs e) {
         if (e.PropertyName == nameof(DTPController.result)) {
            _suspend_notify_vm = true;
            TimeTuple = _controller.result;
            _suspend_notify_vm = false;
         }
      }

      // This is not exactly a view-model, bc it accesses the time TextBox directly so it can get/set the CaretIndex,
      // which is used to determine which time-componenet - hh:mm:ss AM/PM - should be incremented/decremented via the up/down arrow-keys.
      class DTPController : ViewModelBase {
         readonly TextBox _time_text_box;
         TimeTextTuple _result;

         public DTPController(TextBox time_text_box) {
            _time_text_box = time_text_box;
            tick_time = new DelegateCommand<int>(_tick_time);
            tick_month = new DelegateCommand<int>(_tick_month);
            tick_day = new DelegateCommand<int>(_tick_day);
            tick_year = new DelegateCommand<int>(_tick_year);
         }

         public string month_text {
            get => _result.month_text;
            set {
               _result = new TimeTextTuple(value, _result.day_text, _result.year_text, _result.time_text);
               RaisePropertyChanged(nameof(result));
            }
         }
         public string day_text {
            get => _result.day_text;
            set {
               _result = new TimeTextTuple(_result.month_text, value, _result.year_text, _result.time_text);
               RaisePropertyChanged(nameof(result));
            }
         }
         public string year_text {
            get => _result.year_text;
            set {
               _result = new TimeTextTuple(_result.month_text, _result.day_text, value, _result.time_text);
               RaisePropertyChanged(nameof(result));
            }
         }
         public string time_text {
            get => _result.time_text;
            set {
               _result = new TimeTextTuple(_result.month_text, _result.day_text, _result.year_text, value);
               RaisePropertyChanged(nameof(result));
            }
         }
         public TimeTextTuple result {
            get => _result;
            set {
               _result = value;
               RaisePropertyChanged(nameof(month_text), nameof(day_text), nameof(year_text), nameof(time_text));
            }
         }

         public bool use_military_time { get; set; }
         public ICommand tick_time { get; }
         public ICommand tick_month { get; }
         public ICommand tick_day { get; }
         public ICommand tick_year { get; }
         string _time_format => use_military_time ? StringFormats.Time.MilitaryWithSeconds : StringFormats.Time.StandardWithSeconds;

         public void tick_time_component(int increment) {
            _ensure_time_text_up_to_date();
            var r = _result.result;
            if (r.HasValue) {
               int index = _time_text_box.CaretIndex;
               using (detect_property_changes()) {
                  DateTime dt;
                  if (index <= 0) {
                     dt = r.Value.AddHours(increment);
                  } else {
                     int segment = _time_text_box.Text.Take(index).Count(c => c == ':');
                     switch (segment) {
                        case 0: dt = r.Value.AddHours(increment); break;
                        case 1: dt = r.Value.AddMinutes(increment); break;
                        case 2:
                           if (_time_text_box.Text.Take(index).Contains(' ')) {
                              // toggle AM/PM
                              dt = r.Value.AddHours(r.Value.Hour < 12 ? 12 : -12);
                           } else {
                              dt = r.Value.AddSeconds(increment);
                           }
                           break;
                        default:
                           dt = r.Value.AddSeconds(increment);
                           break;
                     }
                  }
                  _result = TimeTextTuple.FromDateTime(dt, _time_format);
               }
               _time_text_box.CaretIndex = index;
            } else {
               using (detect_property_changes())
                  _force_valid_inputs();
            }
         }
 
         void _tick_time(int increment) {
            _ensure_time_text_up_to_date();
            using (detect_property_changes()) {
               var r = _result.result;
               if (r.HasValue) {
                  var dt = r.Value.AddMinutes(increment);
                  _result = TimeTextTuple.FromDateTime(dt);
               } else {
                  _force_valid_inputs();
               }
            }
         }

         void _tick_month(int increment) {
            using (detect_property_changes()) {
               var r = _result.result;
               if (r.HasValue) {
                  var dt = r.Value.AddMonths(increment);
                  _result = TimeTextTuple.FromDateTime(dt);
               } else {
                  _force_valid_inputs();
               }
            }
         }

         void _tick_day(int increment) {
            using (detect_property_changes()) {
               var r = _result.result;
               if (r.HasValue) {
                  var dt = r.Value.AddDays(increment);
                  _result = TimeTextTuple.FromDateTime(dt);
               } else {
                  _force_valid_inputs();
               }
            }
         }

         void _tick_year(int increment) {
            using (detect_property_changes()) {
               var r = _result.result;
               if (r.HasValue) {
                  var dt = r.Value.AddYears(increment);
                  _result = TimeTextTuple.FromDateTime(dt);
               } else {
                  _force_valid_inputs();
               }
            }
         }
 
         void _ensure_time_text_up_to_date() {
            if (_time_text_box.IsKeyboardFocused)
               _time_text_box.GetBindingExpression(TextBox.TextProperty)?.UpdateSource();
         }

         void _force_valid_inputs() {
            var dt = DateTime.Today.Date;
            if (DateTime.TryParse(time_text, out DateTime dt_time))
               dt = dt.Add(dt_time.TimeOfDay);
            _result = TimeTextTuple.FromDateTime(dt);
         }
      }
   }
}