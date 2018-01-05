using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Motorsports.Scaffolding.Core {
  public static partial class Extensions {
    public static async Task<IEnumerable<TResult>> SelectAsync<TSource, TResult>(this IEnumerable<TSource> values, Func<TSource, Task<TResult>> asyncSelector) {
      return await Task.WhenAll(values.Select(asyncSelector));
    }
  }
}