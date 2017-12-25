using System;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq.Expressions;
using System.Reflection;

namespace Motorsports.Scaffolding.Core.Views {
  public static partial class Extensions {
    public static string GetDisplayName(this Enum enumVal) {
      var displayAttrib = enumVal.GetAttributeOfType<DisplayAttribute>();
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

    public static T GetAttributeOfType<T>(this Enum enumVal) where T : Attribute {
      var type = enumVal.GetType();
      var memInfo = type.GetMember(enumVal.ToString());
      var attributes = memInfo[0].GetCustomAttributes(typeof(T), false);
      return attributes.Length > 0
        ? (T) attributes[0]
        : null;
    }

    public static T GetAttribute<T>(this ICustomAttributeProvider provider)
      where T : Attribute {
      var attributes = provider.GetCustomAttributes(typeof(T), true);
      return attributes.Length > 0
        ? attributes[0] as T
        : null;
    }
  }
}