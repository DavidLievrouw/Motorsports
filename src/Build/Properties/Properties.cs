using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using Cake.Core;

namespace Motorsports.Build.Properties {
  public interface IProperties {
    ICakeContext Context { get; }
    IEnumerable<PropertyInfo> PublicProperties { get; }
  }

  public abstract class Properties<TProperties> : IProperties {
    protected Properties(ICakeContext context) {
      Context = context ?? throw new ArgumentNullException(nameof(context));
    }

    public static IEnumerable<PropertyInfo> PublicProperties { get; } = typeof(TProperties).GetProperties(BindingFlags.Public | BindingFlags.Instance).Where(p => p.CanRead);
    public static string ClassName { get; } = typeof(TProperties).GetTypeInfo().Name;

    public ICakeContext Context { get; }
    IEnumerable<PropertyInfo> IProperties.PublicProperties => PublicProperties;

    public override string ToString() {
      var stringBuilder = new StringBuilder();
      stringBuilder.AppendLine(ClassName);
      stringBuilder.AppendLine(new string('-', ClassName.Length));
      var childPropertiesList = new List<IProperties>();
      foreach (var prop in PublicProperties) {
        var propValue = prop.GetValue(this);
        if (propValue is IProperties) childPropertiesList.Add(propValue as IProperties);
        else if (!(propValue is ICakeContext)) stringBuilder.AppendLine($" - {prop.Name}: {propValue ?? "[NULL]"}");
      }

      foreach (var childProperties in childPropertiesList) {
        stringBuilder.AppendLine(childProperties.ToString());
      }

      return Environment.NewLine + stringBuilder.ToString().Trim();
    }
  }
}