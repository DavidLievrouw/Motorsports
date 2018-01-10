using System;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq.Expressions;

namespace Motorsports.Scaffolding.Core {
  public static partial class Extensions {
    public static string GetDisplayName(this Enum enumVal) {
      var displayAttrib = enumVal.GetAttribute<DisplayAttribute>();
      if (displayAttrib == null) return enumVal.ToString();
      return string.IsNullOrEmpty(displayAttrib.Name)
        ? enumVal.ToString()
        : displayAttrib.Name;
    }

    public static string GetDisplayName<T, TMember>(this Expression<Func<T, TMember>> expression) {
      if (!(expression.Body is MemberExpression memberExpression)) throw new InvalidOperationException("Expression must be a member expression");
      var displayAttrib = memberExpression.Member.GetAttribute<DisplayAttribute>();
      if (!string.IsNullOrEmpty(displayAttrib?.Name)) return displayAttrib.Name;
      var displayNameAttrib = memberExpression.Member.GetAttribute<DisplayNameAttribute>();
      if (!string.IsNullOrEmpty(displayNameAttrib?.DisplayName)) return displayNameAttrib.DisplayName;
      return memberExpression.Member.Name;
    }
  }
}