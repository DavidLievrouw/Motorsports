using System;
using System.ComponentModel.DataAnnotations;
using System.Linq.Expressions;

namespace Motorsports.Scaffolding.Core {
  public static partial class Extensions {
    public static string GetNullDisplayText<T, TMember>(this Expression<Func<T, TMember>> expression) {
      if (!(expression.Body is MemberExpression memberExpression)) throw new InvalidOperationException("Expression must be a member expression");
      var displayAttrib = memberExpression.Member.GetAttribute<DisplayFormatAttribute>();
      return string.IsNullOrEmpty(displayAttrib?.NullDisplayText)
        ? null
        : displayAttrib.NullDisplayText;
    }
  }
}