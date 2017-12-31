namespace Motorsports.Scaffolding.Core.Security {
  public struct PlainTextPassword {
    readonly string _value;

    public static PlainTextPassword Empty = new PlainTextPassword();

    public PlainTextPassword(string value) {
      _value = string.IsNullOrWhiteSpace(value)
        ? null
        : value;
    }

    public static implicit operator string(PlainTextPassword instance) {
      return instance._value;
    }

    public bool IsTooShort() {
      return string.IsNullOrEmpty(_value) || _value.Length < 8;
    }
  }
}