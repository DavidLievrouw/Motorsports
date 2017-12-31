using System;
using System.Collections.Generic;
using System.Linq;
using FluentValidation.Results;

namespace Motorsports.Scaffolding.Core.Security {
  public static class ExtensionsForIEnumerableOfValidationFailure {
    public static T[] ParseFailureReasons<T>(this IEnumerable<ValidationFailure> validationFailures) where T : struct {
      return validationFailures
        .Select(failure => failure.ErrorCode)
        .Select(code => Enum.TryParse(code, true, out T result) ? (T?) result : (T?) null)
        .Where(result => result != null)
        .Select(result => result.Value)
        .ToArray();
    }
  }
}