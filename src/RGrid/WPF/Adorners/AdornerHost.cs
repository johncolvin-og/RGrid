using System;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Media;

namespace RGrid.WPF {
   class AdornerHost : Adorner {
      UIElement _child;

      public AdornerHost(UIElement adorned_element)
         : base(adorned_element) { }

      public static readonly DependencyProperty ChildProperty =
         WPFHelper.create_dp<UIElement, AdornerHost>(
            nameof(Child), (o, v) => {
               if (o._child != null)
                  o.RemoveVisualChild(o._child);
               o._child = v;
               if (v != null)
                  o.AddVisualChild(v);
            });

      public UIElement Child {
         get => _child;
         set => SetValue(ChildProperty, value);
      }

      protected override int VisualChildrenCount => _child != null ? 1 : 0;

      protected override Visual GetVisualChild(int index) =>
         index == 0 && _child != null ? _child : throw new ArgumentOutOfRangeException();

      protected override Size ArrangeOverride(Size finalSize) {
         if (_child == null) {
            return base.ArrangeOverride(finalSize);
         }
         _child.Arrange(new Rect(finalSize));
         return _child.DesiredSize;
      }

      protected override Size MeasureOverride(Size constraint) {
         if (_child == null) {
            return base.MeasureOverride(constraint);
         }
         _child.Measure(constraint);
         return _child.DesiredSize;
      }
   }

   class AdornerHost<TChild> : AdornerHost where TChild : UIElement {
      public AdornerHost(UIElement adorned_element)
         : base(adorned_element) { }

      public new TChild Child {
         get => base.Child as TChild;
         set => base.Child = value;
      }
   }
}
