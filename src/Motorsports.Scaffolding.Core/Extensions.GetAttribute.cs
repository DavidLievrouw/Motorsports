using System;
using System.Reflection;

namespace Motorsports.Scaffolding.Core {
  public static partial class Extensions {
    public static T GetAttribute<T>(this Enum enumVal) where T : Attribute {
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