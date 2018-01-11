using System.Collections.Generic;
using System.Linq;

namespace Motorsports.Scaffolding.Core.Models.Validators {
  public class ValidationResult {
    public ValidationResult() {
      Errors = new List<ValidationFailure>();
    }

    public ValidationResult(IEnumerable<ValidationFailure> failures) {
      Errors = failures.Where(failure => failure != null).ToList();
    }
    
    public ValidationResult(params ValidationFailure[] failures) {
      Errors = failures.Where(failure => failure != null).ToList();
    }

    public ValidationResult(FluentValidation.Results.ValidationResult result) {
      Errors = result.Errors.Select(e => new ValidationFailure(e.PropertyName, e.ErrorMessage)).ToList();
    }

    public virtual bool IsValid => Errors.Count == 0;

    public IList<ValidationFailure> Errors { get; }
  }
}