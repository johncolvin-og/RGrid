using System;

namespace RGrid.Filters {
   public interface IMinMaxState<T> where T : struct {
      INullableStructProto<T> minimum { get; }
      INullableStructProto<T> maximum { get; }
   }

   public interface INullableStructProto<T> where T : struct {
      bool active { get; }
      T? value { get; set; }
   }

   public class ComparableMinMax<TRow, T, TState> : FilterVMBase<TRow, T, TState> where T : struct, IComparable, IComparable<T> where TState : IMinMaxState<T>, new() {
      readonly TState state = new TState();

      internal ComparableMinMax(Func<TRow, T> get_row_val, string prop_name)
         : base(get_row_val, prop_name) { }

      public T? minimum { get => state.minimum.value; set => state.minimum.value = value; }
      public T? maximum { get => state.maximum.value; set => state.maximum.value = value; }

      protected override void _clear() {
         _destroy_snapshot();
         _close();
         state.minimum.value = null;
         state.maximum.value = null;
         RaisePropertyChanged(nameof(minimum), nameof(maximum));
         _raise_filter_changed();
      }

      protected override bool _filter(T value) =>
         (!state.minimum.active || value.CompareTo(state.minimum.value.Value) >= 0) && (!state.maximum.active || value.CompareTo(state.maximum.value.Value) <= 0);

      protected override bool _get_active() =>
         state.minimum.active || state.maximum.active;

      public override TState GetState() {
         var state = new TState();
         state.minimum.value = minimum;
         state.maximum.value = maximum;
         return state;
      }

      protected override void _load_state_internal(TState state) {
         if (state != null) {
            this.state.minimum.value = state.minimum.value;
            this.state.maximum.value = state.maximum.value;
         }
      }
   }
}