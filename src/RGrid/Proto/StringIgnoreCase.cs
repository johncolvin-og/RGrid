using EqualityComparer.Extensions.Utilities;
using ProtoBuf;
using System;

namespace RGrid.Proto {
   [ProtoContract]
   public struct StringIgnoreCase : IEquatable<StringIgnoreCase> {
      [ProtoMember(1)]
      readonly string _text;
      [ProtoMember(2)]
      readonly bool _ignore_case;

      public StringIgnoreCase(string text, bool ignore_case) {
         _text = text;
         _ignore_case = ignore_case;
      }

      public string text => _text;
      public bool ignore_case => _ignore_case;

      public StringIgnoreCase with(string text = null, bool? ignore_case = null) =>
         new StringIgnoreCase(text ?? _text, ignore_case ?? _ignore_case);

      public bool Equals(StringIgnoreCase other) =>
         other._ignore_case == _ignore_case && other._text == text;

      public override bool Equals(object obj) =>
         obj is StringIgnoreCase sic && Equals(sic);

      public override int GetHashCode() =>
         HashUtils.Phase(_ignore_case.GetHashCode(), _text?.GetHashCode() ?? -673);

      public static bool operator ==(StringIgnoreCase a, StringIgnoreCase b) => a.Equals(b);
      public static bool operator !=(StringIgnoreCase a, StringIgnoreCase b) => !a.Equals(b);
   }
}