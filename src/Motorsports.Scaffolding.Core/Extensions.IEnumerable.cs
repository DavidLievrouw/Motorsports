using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Motorsports.Scaffolding.Core {
  public static partial class Extensions {
    public static async Task<IEnumerable<TResult>> SelectAsync<TSource, TResult>(this IEnumerable<TSource> values, Func<TSource, Task<TResult>> asyncSelector) {
      return await Task.WhenAll(values.Select(asyncSelector));
    }

    public static TResult MinOrDefault<TResult>(this IEnumerable<TResult> source, TResult defaultValue) where TResult : IComparable {
      using (var en = source.GetEnumerator()) {
        if (en.MoveNext()) {
          var currentMin = en.Current;
          while (en.MoveNext()) {
            var current = en.Current;
            if (current.CompareTo(currentMin) < 0) currentMin = current;
          }

          return currentMin;
        }
      }

      return defaultValue;
    }

    public static TResult MinOrDefault<TResult>(this IEnumerable<TResult> source) where TResult : IComparable {
      return source.MinOrDefault(default(TResult));
    }

    public static TResult MinOrDefault<TSource, TResult>(this IEnumerable<TSource> source, Func<TSource, TResult> selector, TResult defaultValue) where TResult : IComparable {
      return source.Select(selector).MinOrDefault(defaultValue);
    }

    public static TResult MinOrDefault<TSource, TResult>(this IEnumerable<TSource> source, Func<TSource, TResult> selector) where TResult : IComparable {
      return source.Select(selector).MinOrDefault();
    }
    
    public static TResult MaxOrDefault<TResult>(this IEnumerable<TResult> source, TResult defaultValue) where TResult : IComparable {
      using (var en = source.GetEnumerator()) {
        if (en.MoveNext()) {
          var currentMax = en.Current;
          while (en.MoveNext()) {
            var current = en.Current;
            if (current.CompareTo(currentMax) > 0) currentMax = current;
          }

          return currentMax;
        }
      }

      return defaultValue;
    }

    public static TResult MaxOrDefault<TResult>(this IEnumerable<TResult> source) where TResult : IComparable {
      return source.MaxOrDefault(default(TResult));
    }

    public static TResult MaxOrDefault<TSource, TResult>(this IEnumerable<TSource> source, Func<TSource, TResult> selector, TResult defaultValue) where TResult : IComparable {
      return source.Select(selector).MaxOrDefault(defaultValue);
    }

    public static TResult MaxOrDefault<TSource, TResult>(this IEnumerable<TSource> source, Func<TSource, TResult> selector) where TResult : IComparable {
      return source.Select(selector).MaxOrDefault();
    }
  }
}