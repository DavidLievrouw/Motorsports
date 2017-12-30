namespace Motorsports.Scaffolding.Core.Models.Validators {
  public class ValidationFailure {
    public ValidationFailure(string propertyName, string errorMessage) {
      PropertyName = propertyName;
      ErrorMessage = errorMessage;
    }

    public string PropertyName { get; }

    public string ErrorMessage { get; }

    public object[] FormattedMessageArguments { get; set; }
  }
}